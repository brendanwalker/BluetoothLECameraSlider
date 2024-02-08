#include "ConfigManager.h"
#include "SliderManager.h"

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

float SliderState::remapFloatToFloat(
  float inA, float inB, 
  float outA, float outB, 
  float inValue)
{
  if (inB > inA)
  {
    float clampedValue= max(min(inValue, inB), inA);
    float u= (clampedValue - inA) / (inB - inA);  
    float rempappedValue= ((1.f - u)*outA + u*outB);

    return rempappedValue;
  }
  else
  {
    float clampedValue= max(min(inValue, inA), inB);
    float u= (clampedValue - inB) / (inA - inB);  
    float rempappedValue= (u*outA + (1.f - u)*outB);

    return rempappedValue;
  }
}

int32_t SliderState::remapFloatToInt32(
  float floatMin, float floatMax, 
  int32_t intMin, int32_t intMax, 
  float inValue)
{
  return (int32_t)remapFloatToFloat(floatMin, floatMax, (float)intMin, (float)intMax, inValue);
}

float SliderState::remapInt32ToFloat(
  int32_t intMin, int32_t intMax, 
  float floatMin, float floatMax, 
  int32_t inValue)
{
  return remapFloatToFloat((float)intMin, (float)intMax, floatMin, floatMax, (float)inValue);
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

float SliderState::getPanStepperAngularAcceleration()
{  
  float motorAngAccelDegrees= stepsToMotorAngle(m_panStepper->getAcceleration());
  float cameraAngAccelDegrees= motorAngAccelDegrees / PAN_GEAR_RATIO;

  return cameraAngAccelDegrees;
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

float SliderState::getTiltStepperAngularAcceleration()
{
  float motorAngAccelDegrees= stepsToMotorAngle(m_tiltStepper->getAcceleration());
  float cameraAngAccelDegrees= motorAngAccelDegrees / TILT_GEAR_RATIO;

  return cameraAngAccelDegrees;
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

float SliderState::getSlideStepperLinearAcceleration()
{
  const float motorAngAccelDegrees= stepsToMotorAngle(m_tiltStepper->getAcceleration());
  const float motorAngAccelRadians= motorAngAccelDegrees * (PI / 180.f);
  const float cameraLinAccelMM= motorAngAccelRadians * SLIDER_GEAR_RADIUS;

  return cameraLinAccelMM;
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

float SliderState::getPanStepperAngularSpeed()
{
  const float speedInHz= (float)m_panStepper->getSpeedInMilliHz() / 1000.f;
  const float motorDegreesPerSecond= stepsToMotorAngle(speedInHz);
  const float cameraDegreesPerSecond= motorDegreesPerSecond / PAN_GEAR_RATIO;

  return cameraDegreesPerSecond;
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

float SliderState::getTiltStepperAngularSpeed()
{
  const float speedInHz= (float)m_tiltStepper->getSpeedInMilliHz() / 1000.f;
  const float motorDegreesPerSecond= stepsToMotorAngle(speedInHz);
  const float cameraDegreesPerSecond= motorDegreesPerSecond / TILT_GEAR_RATIO;

  return cameraDegreesPerSecond;
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

float SliderState::getSlideStepperLinearSpeed()
{
  const float speedInHz= (float)m_slideStepper->getSpeedInMilliHz() / 1000.f;
  const float turnDegreesPerSecond= stepsToMotorAngle(speedInHz);
  const float turnRadiansPerSecond= turnDegreesPerSecond * (PI / 180.f);
  const float cameraMMPerSecond= turnRadiansPerSecond * SLIDER_GEAR_RADIUS;

  return cameraMMPerSecond;
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

float SliderState::getPanStepperDegrees() const
{
  int32_t rawPosition= getPanStepperPosition();
  float motorDegrees = stepsToMotorAngle(rawPosition - m_panStepperCenter);
  float cameraDegrees = motorDegrees / PAN_GEAR_RATIO;
  float clampedCameraDegrees = max(min(cameraDegrees, PAN_MAX_ANGLE), PAN_MIN_ANGLE);  

  return clampedCameraDegrees;
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

float SliderState::getTiltStepperDegrees() const
{
  int32_t rawPosition= getTiltStepperPosition();
  float motorDegrees = stepsToMotorAngle(rawPosition - m_tiltStepperCenter);
  float cameraDegrees = motorDegrees / TILT_GEAR_RATIO;
  float clampedCameraDegrees = max(min(cameraDegrees, TILT_MAX_ANGLE), TILT_MIN_ANGLE);  

  return clampedCameraDegrees;
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

void SliderState::setSliderPosFraction(float fraction)
{
  // Remap [-1, 1] unit position to calibrated slider min and max step position  
  int32_t newTargetSliderPosition= remapFloatToInt32(-1.f, 1.f, m_sliderStepperMin, m_sliderStepperMax, fraction);

  if (newTargetSliderPosition != m_lastTargetSlidePosition)
  {
    m_slideStepper->moveTo(newTargetSliderPosition);

    // Remember new target slide position
    Serial.printf("New Slide Position Target: %d -> %d\n", m_lastTargetSlidePosition, newTargetSliderPosition);
    m_lastTargetSlidePosition= newTargetSliderPosition;      
  }
}

float SliderState::getSliderPosFraction()
{
  return remapInt32ToFloat(m_sliderStepperMin, m_sliderStepperMax, -1.f, 1.f, getSlideStepperPosition());
}

void SliderState::setSliderSpeedFraction(float fraction)
{
  // Remap [0, 1] unit speed to mm/s
  // (special case for 0: use 0 speed to stop the slider rather than SLIDE_MIN_SPEED)
  float newTargetSliderSpeed= fraction > 0.f ? remapFloatToFloat(0.f, 1.f, SLIDE_MIN_SPEED, SLIDE_MAX_SPEED, fraction) : 0.f;
  setSlideStepperLinearSpeed(newTargetSliderSpeed);
}

float SliderState::getSliderSpeedFraction()
{
  return remapFloatToFloat(SLIDE_MIN_SPEED, SLIDE_MAX_SPEED, 0.f, 1.f, getSlideStepperLinearSpeed());
}

void SliderState::setSliderAccelFraction(float fraction)
{
  // Remap [0, 1] unit accel to mm/s^2
  float newTargetSliderAccel= remapFloatToFloat(0.f, 1.f, SLIDE_MIN_ACCELERATION, SLIDE_MAX_ACCELERATION, fraction);
  setSlideStepperLinearAcceleration(newTargetSliderAccel);
}

float SliderState::getSliderAccelFraction()
{
  return remapFloatToFloat(SLIDE_MIN_ACCELERATION, SLIDE_MAX_ACCELERATION, 0.f, 1.f, getSlideStepperLinearAcceleration());
}

void SliderState::setPanPosFraction(float fraction)
{
  float newTargetDegrees= remapFloatToFloat(-1.f, 1.f, PAN_MIN_ANGLE, PAN_MAX_ANGLE, fraction);
  setPanStepperTargetDegrees(newTargetDegrees);
}

float SliderState::getPanPosFraction()
{
  return remapFloatToFloat(PAN_MIN_ANGLE, PAN_MAX_ANGLE, -1.f, 1.f, getPanStepperDegrees());
}

void SliderState::setPanSpeedFraction(float fraction)
{
  float newTargetSpeed= fraction > 0.f ? remapFloatToFloat(0.f, 1.f, PAN_MIN_SPEED, PAN_MAX_SPEED, fraction) : 0.f;
  setPanStepperAngularSpeed(newTargetSpeed);
}

float SliderState::getPanSpeedFraction()
{
  return remapFloatToFloat(PAN_MIN_SPEED, PAN_MAX_SPEED, 0.f, 1.f, getPanStepperAngularSpeed());
}

void SliderState::setPanAccelFraction(float fraction)
{
  float newTargetAccel= remapFloatToFloat(0.f, 1.f, PAN_MIN_ACCELERATION, PAN_MAX_ACCELERATION, fraction);
  setPanStepperAngularAcceleration(newTargetAccel);
}

float SliderState::getPanAccelFraction()
{
  return remapFloatToFloat(PAN_MIN_ACCELERATION, PAN_MAX_ACCELERATION, 0.f, 1.f, getPanStepperAngularAcceleration());
}

void SliderState::setTiltPosFraction(float fraction)
{
  int32_t newTargetDegrees= remapFloatToFloat(-1.f, 1.f, TILT_MIN_ANGLE, TILT_MAX_ANGLE, fraction);
  setTiltStepperTargetDegrees(newTargetDegrees);
}

float SliderState::getTiltPosFraction()
{
  return remapFloatToFloat(TILT_MIN_ANGLE, TILT_MAX_ANGLE, -1.f, 1.f, getTiltStepperDegrees());
  // float tiltStepperPos= getTiltStepperDegrees();
  // float fraction= remapFloatToFloat(TILT_MIN_ANGLE, TILT_MAX_ANGLE, -1.f, 1.f, tiltStepperPos);
  // Serial.printf("getTiltPosFraction: %.2f[%.2f, %.2f] -> %.2f\n", tiltStepperPos, TILT_MIN_ANGLE, TILT_MAX_ANGLE, fraction);  
  //return fraction;
}

void SliderState::setTiltSpeedFraction(float fraction)
{
  float newTargetSpeed= fraction > 0.f ? remapFloatToFloat(-1.f, 1.f, TILT_MIN_SPEED, TILT_MAX_SPEED, fraction) : 0.f;
  setTiltStepperAngularSpeed(newTargetSpeed);
}

float SliderState::getTiltSpeedFraction()
{
  return remapFloatToFloat(TILT_MIN_SPEED, TILT_MAX_SPEED, 0.f, 1.f, getTiltStepperAngularSpeed());
}

void SliderState::setTiltAccelFraction(float fraction)
{
  float newTargetAccel= remapFloatToFloat(0.f, 1.f, TILT_MIN_ACCELERATION, TILT_MAX_ACCELERATION, fraction);
  setTiltStepperAngularAcceleration(newTargetAccel);
}

float SliderState::getTiltAccelFraction()
{
  return remapFloatToFloat(TILT_MIN_ACCELERATION, TILT_MAX_ACCELERATION, 0.f, 1.f, getTiltStepperAngularAcceleration());
}