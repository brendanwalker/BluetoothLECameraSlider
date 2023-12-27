#include "ConfigManager.h"
#include "SliderManager.h"
#include "NetworkManager.h"

#define PAN_MIN_ANGLE -180.f // degrees
#define PAN_MAX_ANGLE 180.f // degrees
#define PAN_MIN_SPEED 5.f // degrees / s
#define PAN_MAX_SPEED 250.f // degrees / s
#define PAN_MIN_ACCELERATION 100.f // degrees / s²
#define PAN_MAX_ACCELERATION 1500.f // degrees / s²

#define TILT_MIN_ANGLE -90.f // degrees
#define TILT_MAX_ANGLE 45.f // degrees
#define TILT_MIN_SPEED 5.f // degrees / s
#define TILT_MAX_SPEED 250.f // degrees / s
#define TILT_MIN_ACCELERATION 100.f // degrees / s²
#define TILT_MAX_ACCELERATION 1500.f // degrees / s²

#define SLIDE_MIN_SPEED 5.f // mm / s
#define SLIDE_MAX_SPEED 300.f // mm / s
#define SLIDE_MIN_ACCELERATION 10.f // mm / s²
#define SLIDE_MAX_ACCELERATION 350.f // mm / s²

#define MOTOR_DEGREES_PER_STEP 1.8f // degrees / step for StepperOnline Part No. 17HS19-2004S1
// https://www.analog.com/media/en/technical-documentation/data-sheets/TMC2202_TMC2208_TMC2224_datasheet_rev1.13.pdf
// When MS1 and MS2 are disconnected the TMC2208 stepper motor driver makes 8 microsteps / step
#define DRIVER_MICROSTEP_PER_STEP 8.f
#define DEGREES_PER_STEP (MOTOR_DEGREES_PER_STEP / DRIVER_MICROSTEP_PER_STEP)
#define STEPS_PER_DEGREE (1.f/DEGREES_PER_STEP)

// Gear
#define SLIDER_GEAR_DIAMETER 45.f // SLIDE_GT2_Gear_Pulley_72z.stl 
#define SLIDER_GEAR_RADIUS (SLIDER_GEAR_DIAMETER * 0.5f)
#define TILT_GEAR_RATIO (64.f / 21.f) // Driven Gear(TILT_64_Tooth_Herringbone_Gear.stl) / Driver Gear (TILT_21_Tooth_Herringbone_Gear_Flexi.stl)
#define PAN_GEAR_RATIO (144.f / 17.f) // Driven Gear(PAN_144_Tooth_Herringbone_Gear_Base_Mount.stl) / Driver Gear (PAN_17_Tooth_Herringbone_Gear_Flexi.stl)

SliderState *SliderState::s_instance = nullptr;

SliderState::SliderState(
    uint8_t en_pin,
    uint8_t pan_step_pin, uint8_t pan_dir_pin,
    uint8_t tilt_step_pin, uint8_t tilt_dir_pin,
    uint8_t slide_step_pin, uint8_t slide_dir_pin)
    : m_enPin(en_pin)
    , m_panStepPin(pan_step_pin)
    , m_panDirPin(pan_dir_pin)
    , m_tiltStepPin(tilt_step_pin)
    , m_tiltDirPin(tilt_dir_pin)
    , m_slideStepPin(slide_step_pin)
    , m_slideDirPin(slide_dir_pin)
{
  s_instance = this;

  m_lastTargetSlidePosition= 0; // step position
  m_lastTargetSlideSpeed= 0; // steps / s
  m_lastTargetSlideAcceleration= 0.f; // steps / s²

  m_lastTargetPanPosition= 0; // step position
  m_lastTargetPanSpeed= 0; // steps / s
  m_lastTargetPanAcceleration= 0; // steps / s²

  m_lastTargetTiltPosition= 0; // step position
  m_lastTargetTiltSpeed= 0; // steps / s
  m_lastTargetTiltAcceleration= 0; // steps / s²
}

void SliderState::setup()
{
  m_engine.init();
  m_panStepper = m_engine.stepperConnectToPin(m_panStepPin);
  m_tiltStepper = m_engine.stepperConnectToPin(m_tiltStepPin);
  m_slideStepper = m_engine.stepperConnectToPin(m_slideStepPin);

  if (m_panStepper)
  {
    m_panStepper->setDirectionPin(m_panDirPin);
    m_panStepper->setEnablePin(m_enPin);
    m_panStepper->setAutoEnable(true);

    m_panStepper->setSpeedInHz(PAN_MAX_SPEED * 0.5f);
    m_panStepper->setAcceleration(PAN_MAX_ACCELERATION  * 0.5f);
  }
  else
  {
    Serial.println("Failed to create Pan Stepper");
  }

  if (m_tiltStepper)
  {
    m_tiltStepper->setDirectionPin(m_tiltDirPin);
    m_tiltStepper->setEnablePin(m_enPin);
    m_tiltStepper->setAutoEnable(true);

    m_tiltStepper->setSpeedInHz(TILT_MAX_SPEED * 0.5f);
    m_tiltStepper->setAcceleration(TILT_MAX_ACCELERATION * 0.5f);
  }
  else
  {
    Serial.println("Failed to create Tilt Stepper");
  }

  if (m_slideStepper)
  {
    m_slideStepper->setDirectionPin(m_slideDirPin);
    m_slideStepper->setEnablePin(m_enPin);
    m_slideStepper->setAutoEnable(true);

    m_slideStepper->setSpeedInHz(SLIDE_MAX_SPEED * 0.5f);
    m_slideStepper->setAcceleration(SLIDE_MAX_ACCELERATION * 0.5f);
  }
  else
  {
    Serial.println("Failed to create Slide Stepper");
  }

  // Load saved slider config settings
  ConfigManager* configManager= ConfigManager::getInstance();

  StepperMotorCalibration calibration= {};
  if (configManager->getMotorCalibrationConfig(calibration))
  {
    m_panStepperCenter= calibration.panStepperCenter;
    m_tiltStepperCenter= calibration.tiltStepperCenter;
    m_sliderStepperMin= calibration.slideStepperMin;
    m_sliderStepperMax= calibration.slideStepperMax;

    StepperMotorPosition positions= {};
    if (configManager->getMotorPositionConfig(positions))
    {
      m_panStepper->setCurrentPosition(positions.panStepperPosition);
      m_tiltStepper->setCurrentPosition(positions.tiltStepperPosition);
      m_slideStepper->setCurrentPosition(positions.slideStepperPosition);

      // Mark the slider as being calibrated and in a known configuration
      m_calibrated= true;
    }
  }  

  // Listen for artnet packets from the network
  ArtnetWiFiReceiver &artnet = NetworkManager::getInstance()->getArtnetReceiver();
  artnet.subscribe(
      m_universeId,
      [this](const uint8_t *data, const uint16_t size)
      {
        if (size >= sizeof(ArtnetPacket))
        {
          this->parseArtnetPacket((ArtnetPacket *)data);
        }
      });
}

void SliderState::loop()
{
  ConfigManager* configManager= ConfigManager::getInstance();

  configManager->setMotorPanPosition(m_panStepper->getCurrentPosition());
  configManager->setMotorTiltPosition(m_tiltStepper->getCurrentPosition());
  configManager->setMotorSlidePosition(m_slideStepper->getCurrentPosition());
}

void SliderState::stopAll()
{
  m_panStepper->stopMove();
  m_tiltStepper->stopMove();
  m_slideStepper->stopMove();
}

bool SliderState::isAnySliderRunning() const
{
  return m_panStepper->isRunning() || m_tiltStepper->isRunning() || m_slideStepper->isRunning();
}

void SliderState::writeCalibrationToConfig()
{
    ConfigManager* configManager= ConfigManager::getInstance();

    StepperMotorCalibration calibration= {};
    calibration.panStepperCenter= m_panStepperCenter;
    calibration.tiltStepperCenter= m_tiltStepperCenter;
    calibration.slideStepperMin= m_sliderStepperMin;
    calibration.slideStepperMax= m_sliderStepperMax;

    configManager->setMotorCalibrationConfig(calibration);  
}

void SliderState::writePositionsToConfig()
{
    ConfigManager* configManager= ConfigManager::getInstance();

    configManager->setMotorPanPosition(m_panStepper->getCurrentPosition());
    configManager->setMotorTiltPosition(m_tiltStepper->getCurrentPosition());
    configManager->setMotorSlidePosition(m_slideStepper->getCurrentPosition());
    // Will no-op if none of the positions are actually changed
    configManager->saveMotorPositionConfig();  
}

void SliderState::finalizeCalibration()
{  
  writeCalibrationToConfig();

  // Mark calibration as complete
  m_calibrated= true;
}

float SliderState::remapUInt8ToFloat(
  uint8_t intMin, uint8_t intMax, 
  float floatMin, float floatMax, 
  uint8_t value)
{
  uint8_t clampedValue= max(min(value, intMax), intMin);
  float u= ((float)clampedValue - (float)intMin) / ((float)intMax - (float)intMin);  
  float rempappedValue= (1.f - u)*floatMin + u*floatMax;

  return rempappedValue;
}

int32_t SliderState::remapFloatToInt32(
  float floatMin, float floatMax, 
  int32_t intMin, int32_t intMax, 
  float value)
{
  float clampedValue= max(min(value, floatMax), floatMin);
  float u= (clampedValue - floatMin) / (floatMax - floatMin);  
  int32_t rempappedValue= (int32_t)((1.f - u)*float(intMin) + u*float(intMax));

  return rempappedValue;
}

uint32_t SliderState::motorAngleToSteps(float degrees) const
{
  return (uint32_t)(degrees * STEPS_PER_DEGREE);
}

float SliderState::stepsToMotorAngle(uint32_t steps) const
{
  return (float)steps * DEGREES_PER_STEP;
}

void SliderState::setPanStepperAngularAcceleration(float cameraAngAccelDegrees)
{
  // Use Pan gear ratio to determine motor angular accel from given camera angular accel
  float motorAngAccelDegrees = cameraAngAccelDegrees * PAN_GEAR_RATIO;
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetPanAcceleration= motorAngleToSteps(motorAngAccelDegrees);

  if (newTargetPanAcceleration != m_lastTargetPanAcceleration)
  {
    m_panStepper->setAcceleration(newTargetPanAcceleration);

    // Remember new target pan step acceleration
    Serial.printf("New Pan Accel Target: %d -> %d\n", m_lastTargetPanAcceleration, newTargetPanAcceleration);
    m_lastTargetPanAcceleration= newTargetPanAcceleration;       
  } 
}

void SliderState::setTiltStepperAngularAcceleration(float cameraAngAccelDegrees)
{
  // Use Tilt gear ratio to determine motor angular speed from given camera angular speed
  float motorAngAccelDegrees = cameraAngAccelDegrees * TILT_GEAR_RATIO;
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetTiltAcceleration= motorAngleToSteps(motorAngAccelDegrees);

  if (newTargetTiltAcceleration != m_lastTargetTiltAcceleration)
  {
    m_tiltStepper->setAcceleration(newTargetTiltAcceleration);

    // Remember new target tilt step acceleration
    Serial.printf("New Tilt Accel Target: %d -> %d\n", m_lastTargetTiltAcceleration, newTargetTiltAcceleration);
    m_lastTargetTiltAcceleration= newTargetTiltAcceleration;        
  }
}

void SliderState::setSlideStepperLinearAcceleration(float cameraLinAccelMM)
{
  // Compute degrees/second² to turn to make belt move given number of mm/s² (assuming a pulley radius)
  // Radian/sec² = Arc Length Per Sec² / Radius  
  const float motorAngAccelRadians= (cameraLinAccelMM / SLIDER_GEAR_RADIUS);
  const float motorAngAccelDegrees= motorAngAccelRadians * (180.f / PI);    
  // degrees/sec² * steps/degree = steps/sec²
  int32_t newTargetSlideAcceleration= motorAngleToSteps(motorAngAccelDegrees);

  if (newTargetSlideAcceleration != m_lastTargetSlideAcceleration)
  {  
    m_slideStepper->setAcceleration(newTargetSlideAcceleration);

    // Remember new target slide step acceleration
    Serial.printf("New Slide Accel Target: %d -> %d\n", m_lastTargetSlideAcceleration, newTargetSlideAcceleration);
    m_lastTargetSlideAcceleration= newTargetSlideAcceleration;        
  }
}

void SliderState::setPanStepperAngularSpeed(float cameraDegreesPerSecond)
{
  // Use Pan gear ratio to determine motor angular speed from given camera angular speed
  float motorDegreesPerSecond = cameraDegreesPerSecond * PAN_GEAR_RATIO;
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetPanSpeed= motorAngleToSteps(motorDegreesPerSecond);

  if (newTargetPanSpeed != m_lastTargetPanSpeed)
  {
    m_panStepper->setSpeedInHz(newTargetPanSpeed);

    // Remember new target pan step speed
    Serial.printf("New Pan Speed Target: %d -> %d\n", m_lastTargetPanSpeed, newTargetPanSpeed);
    m_lastTargetPanSpeed= newTargetPanSpeed;
  }
}

void SliderState::setTiltStepperAngularSpeed(float cameraDegreesPerSecond)
{
  // Use Tilt gear ratio to determine motor angular speed from given camera angular speed
  float motorDegreesPerSecond = cameraDegreesPerSecond * TILT_GEAR_RATIO;
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetTiltSpeed= motorAngleToSteps(motorDegreesPerSecond);

  if (newTargetTiltSpeed != m_lastTargetTiltSpeed)
  {
    m_tiltStepper->setSpeedInHz(newTargetTiltSpeed);

    // Remember new target tilt angle angular speed
    Serial.printf("New Tilt Speed Target: %d -> %d\n", m_lastTargetTiltSpeed, newTargetTiltSpeed);
    m_lastTargetTiltSpeed= newTargetTiltSpeed;    
  }
}

void SliderState::setSlideStepperLinearSpeed(float cameraMMPerSecond)
{
  // Compute degrees/second to turn to make belt move given number of mm/s (assuming a pulley radius)
  // Radian/sec = Arc Length Per Sec / Radius  
  const float turnRadiansPerSecond= (cameraMMPerSecond / SLIDER_GEAR_RADIUS);
  const float turnDegreesPerSecond= turnRadiansPerSecond * (180.f / PI);    
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetSlideSpeed= motorAngleToSteps(turnDegreesPerSecond);

  if (newTargetSlideSpeed != m_lastTargetSlideSpeed)
  {
    m_slideStepper->setSpeedInHz(newTargetSlideSpeed);

    // Remember new target slider linear speed
    Serial.printf("New Slide Speed Target: %d -> %d\n", m_lastTargetSlideSpeed, newTargetSlideSpeed);
    m_lastTargetSlideSpeed= newTargetSlideSpeed;        
  }
}

void SliderState::setPanStepperTargetDegrees(float cameraDegrees)
{
  // Use Pan gear ratio to determine motor target angle from given camera angle
  float clampedCameraDegrees = max(min(cameraDegrees, PAN_MAX_ANGLE), PAN_MIN_ANGLE);
  float motorDegrees = clampedCameraDegrees * PAN_GEAR_RATIO;
  // degrees*steps/degree + center step position = target step position
  int32_t newTargetPanPosition= motorAngleToSteps(motorDegrees) + m_panStepperCenter;

  if (newTargetPanPosition != m_lastTargetPanPosition)
  {
    m_panStepper->moveTo(newTargetPanPosition);

    // Remember new target pan position
    Serial.printf("New Pan Position Target: %d -> %d\n", m_lastTargetPanPosition, newTargetPanPosition);
    m_lastTargetPanPosition= newTargetPanPosition;
  }
}

void SliderState::setTiltStepperTargetDegrees(float cameraDegrees)
{
  // Use Tilt gear ratio to determine motor target angle from given camera angle
  float clampedCameraDegrees = max(min(cameraDegrees, TILT_MAX_ANGLE), TILT_MIN_ANGLE);
  float motorDegrees = clampedCameraDegrees * TILT_GEAR_RATIO;
  // degrees*steps/degree + center step position = target step position
  int32_t newTargetTiltPosition= motorAngleToSteps(motorDegrees) + m_tiltStepperCenter;

  if (newTargetTiltPosition != m_lastTargetTiltPosition)
  {
    m_tiltStepper->moveTo(newTargetTiltPosition);

    // Remember new target tilt position
    Serial.printf("New Tilt Position Target: %d -> %d\n", m_lastTargetTiltPosition, newTargetTiltPosition);
    m_lastTargetTiltPosition= newTargetTiltPosition;
  }
}

void SliderState::setSlideStepperTargetUnitPosition(float unitPosition)
{
  // Remap [-1, 1] unit position to calibrated slider min and max step position  
  int32_t newTargetSliderPosition= remapFloatToInt32(-1.f, 1.f, m_sliderStepperMin, m_sliderStepperMax, unitPosition);

  if (newTargetSliderPosition != m_lastTargetSlidePosition)
  {
    m_slideStepper->moveTo(newTargetSliderPosition);

    // Remember new target slide position
    Serial.printf("New Slide Position Target: %d -> %d\n", m_lastTargetSlidePosition, newTargetSliderPosition);
    m_lastTargetSlidePosition= newTargetSliderPosition;      
  }
}

int8_t SliderState::movePanStepperDegrees(float degrees)
{
  const int32_t steps= motorAngleToSteps(degrees * PAN_GEAR_RATIO);
  Serial.printf("Moving Pan %f degrees (%d steps)\n", degrees, steps);

  return m_panStepper->move(steps);
}

int8_t SliderState::moveTiltStepperDegrees(float degrees)
{
  const int32_t steps= motorAngleToSteps(degrees * TILT_GEAR_RATIO);
  Serial.printf("Moving Tilt %f degrees (%d steps)\n", degrees, steps);

  return m_tiltStepper->move(steps);
}

int8_t SliderState::moveSlideStepperMillimeters(float millimeters)
{
  // Compute degrees to turn to make belt move given number of mm (assuming a pulley radius)
  // Radians = Arc Length / Radius
  const float turnRadians= (millimeters / SLIDER_GEAR_RADIUS);
  const float turnDegrees= turnRadians * (180.f / PI);  
  const int32_t steps= motorAngleToSteps(turnDegrees);
  Serial.printf("Moving Slide %f mm (%d steps)\n", millimeters, steps);  

  return m_slideStepper->move(steps);
}

float SliderState::getPanStepperDegreesRange() const
{
  return PAN_MAX_ANGLE - PAN_MIN_ANGLE;
}

float SliderState::getTiltStepperDegreesRange() const
{
  return TILT_MAX_ANGLE - TILT_MIN_ANGLE;
}

float SliderState::getSlideStepperWidthMillimeters() const
{
  // Compute mm belt will move given a number of motor steps (assuming a pulley radius)
  // Arc Length(s) = Radius * Radians 
  const float turnDegrees= stepsToMotorAngle(m_sliderStepperMax - m_sliderStepperMin);
  const float turnRadians= turnDegrees * (PI / 180.f);
  const float millimeters= SLIDER_GEAR_RADIUS * turnRadians;  

  return millimeters;
}

bool SliderState::parseArtnetPacket(const ArtnetPacket *packet)
{
  // ignore incoming artnet packets if we aren't calibrated or processing is explicitly disabled
  if (!m_calibrated || !m_artNetCommandsEnabled)
    return false;

  // Compute 1-byte fixed point position fractions to float value in the range [-1, 1]
  float targetSliderUnitPosition= remapUInt8ToFloat(0, 255, -1.f, 1.f, packet->slidePosition);

  // Compute 1-byte fixed point angles fractions to float values in the range [-1, 1]
  float targetPanAngleDegree= remapUInt8ToFloat(0, 255, PAN_MIN_ANGLE, PAN_MAX_ANGLE, packet->panAngle);
  float targetTiltAngleDegree= remapUInt8ToFloat(0, 255, TILT_MIN_ANGLE, TILT_MAX_ANGLE, packet->tiltAngle);  

  setSlideStepperTargetUnitPosition(targetSliderUnitPosition);
  setPanStepperTargetDegrees(targetPanAngleDegree);
  setTiltStepperTargetDegrees(targetTiltAngleDegree);

  // Compute 1-byte fixed point speed fractions to float speed values
  float targetSliderSpeed= remapUInt8ToFloat(0, 255, SLIDE_MIN_SPEED, SLIDE_MAX_SPEED, packet->slideSpeed);
  float targetPanSpeed= remapUInt8ToFloat(0, 255, PAN_MIN_SPEED, PAN_MAX_SPEED, packet->panSpeed);
  float targetTiltSpeed= remapUInt8ToFloat(0, 255, TILT_MIN_SPEED, TILT_MAX_SPEED, packet->tiltSpeed);

  setSlideStepperLinearSpeed(targetSliderSpeed);
  setPanStepperAngularSpeed(targetPanSpeed);
  setTiltStepperAngularSpeed(targetTiltSpeed);

  // Compute 1-byte fixed point acceleration fractions to float acceleration values
  float targetSliderAcceleration= remapUInt8ToFloat(0, 255, SLIDE_MIN_ACCELERATION, SLIDE_MAX_ACCELERATION, packet->slideAcceleration);
  float targetPanAcceleration= remapUInt8ToFloat(0, 255, PAN_MIN_ACCELERATION, PAN_MAX_ACCELERATION, packet->panAcceleration);
  float targetTiltAcceleration= remapUInt8ToFloat(0, 255, TILT_MIN_ACCELERATION, TILT_MAX_ACCELERATION, packet->tiltAcceleration);

  setSlideStepperLinearAcceleration(targetSliderAcceleration);
  setPanStepperAngularAcceleration(targetPanAcceleration);
  setTiltStepperAngularAcceleration(targetTiltAcceleration);

  return true;
}