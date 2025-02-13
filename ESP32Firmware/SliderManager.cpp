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
  m_lastTargetPanPosition= 0; // step position
  m_lastTargetTiltPosition= 0; // step position

  m_lastSpeedFraction= 0; // [0, 1]
  m_lastAccelerationFraction= 0; // [0, 1]
}

void SliderState::setListener(SliderStateEventListener *listener)
{
    m_listener= listener;
}

void SliderState::clearListener(SliderStateEventListener *listener)
{
  if (m_listener == listener)
    m_listener= nullptr;
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

  int32_t motorPanPos= m_panStepper->getCurrentPosition();
  int32_t motorTiltPos= m_tiltStepper->getCurrentPosition();
  int32_t motorSlidePos= m_slideStepper->getCurrentPosition();

  configManager->setMotorPanPosition(motorPanPos);
  configManager->setMotorTiltPosition(motorTiltPos);
  configManager->setMotorSlidePosition(motorSlidePos);

  if (m_isMovingToTarget)
  {
    if (abs(motorSlidePos - m_lastTargetSlidePosition) <= 10 && 
        abs(motorPanPos - m_lastTargetPanPosition) <= 10 && 
        abs(motorTiltPos - m_lastTargetTiltPosition) <= 10)
      {
        Serial.printf("Finished move to: Slide=%d, Pan=%d, Tilt=%d\n", motorSlidePos, motorPanPos, motorTiltPos);
        setIsMovingToTargetFlag(false);        
      }
  }
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

void SliderState::saveSlideStepperPosAsMin()
{
  int32_t pos= m_slideStepper->getCurrentPosition();

  if (pos != m_sliderStepperMin)
  {
    setSlideStepperMin(pos);
    writeCalibrationToConfig();

    // Tell any clients that the slider min pos changed
    m_listener->onSliderMinSet(pos);
  }
}

void SliderState::saveSlideStepperPosAsMax()
{
  int32_t pos= m_slideStepper->getCurrentPosition();

  if (pos != m_sliderStepperMax)
  {
    setSlideStepperMax(pos);
    writeCalibrationToConfig();

    // Tell any clients that the slider max pos changed
    m_listener->onSliderMaxSet(pos);    
  }
}

void SliderState::resetCalibration()
{  
  m_panStepperCenter= DEFAULT_INITIAL_PAN_POSITION;
  m_tiltStepperCenter= DEFAULT_INITIAL_TILT_POSITION;
  m_sliderStepperMin= DEFAULT_INITIAL_SLIDE_POSITION - MAX_SLIDER_SEARCH_STEPS;
  m_sliderStepperMax= DEFAULT_INITIAL_SLIDE_POSITION + MAX_SLIDER_SEARCH_STEPS;

  m_panStepper->setCurrentPosition(DEFAULT_INITIAL_PAN_POSITION);
  m_tiltStepper->setCurrentPosition(DEFAULT_INITIAL_TILT_POSITION);
  m_slideStepper->setCurrentPosition(DEFAULT_INITIAL_SLIDE_POSITION);
    
  writeCalibrationToConfig();
  writePositionsToConfig();
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

int32_t SliderState::motorAngleToSteps(float degrees) const
{
  return (uint32_t)(degrees * STEPS_PER_DEGREE);
}

float SliderState::stepsToMotorAngle(int32_t steps) const
{
  return (float)steps * DEGREES_PER_STEP;
}

void SliderState::setPanStepperAngularAcceleration(float cameraAngAccelDegrees)
{
  // Use Pan gear ratio to determine motor angular accel from given camera angular accel
  float motorAngAccelDegrees = cameraAngAccelDegrees * PAN_GEAR_RATIO;
  // degrees/sec * steps/degree = steps/sec = speed in Hz
  int32_t newTargetPanAcceleration= motorAngleToSteps(motorAngAccelDegrees);

  m_panStepper->setAcceleration(newTargetPanAcceleration);
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

  m_tiltStepper->setAcceleration(newTargetTiltAcceleration);
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

  m_slideStepper->setAcceleration(newTargetSlideAcceleration);
}

float SliderState::getSlideStepperLinearAcceleration()
{
  const int32_t rawStepsAccel= (int32_t)m_slideStepper->getAcceleration();
  const float motorAngAccelDegrees= stepsToMotorAngle(rawStepsAccel);
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

  m_panStepper->setSpeedInHz(newTargetPanSpeed);
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

  m_tiltStepper->setSpeedInHz(newTargetTiltSpeed);
}

float SliderState::getTiltStepperAngularSpeed()
{
  const int32_t speedInMilliHz= m_tiltStepper->getSpeedInMilliHz();
  const float speedInHz= (float)speedInMilliHz / 1000.f;
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

  m_slideStepper->setSpeedInHz(newTargetSlideSpeed);
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
    setIsMovingToTargetFlag(true);

    // Tell any clients what the final target pan position is
    m_listener->onPanTargetSet(newTargetPanPosition);
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

void SliderState::setIsMovingToTargetFlag(bool flag)
{
  if (!m_isMovingToTarget && flag)
  {
    m_isMovingToTarget= true;

    if (m_listener != nullptr)
      m_listener->onMoveToTargetStart();
  }
  else if (m_isMovingToTarget && !flag)
  {
    m_isMovingToTarget= false;

    if (m_listener != nullptr)
      m_listener->onMoveToTargetComplete();
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
    setIsMovingToTargetFlag(true);

    // Tell any clients what the final target tilt position is
    m_listener->onPanTargetSet(newTargetTiltPosition);
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

void SliderState::setSlideStepperPosition(int32_t newTargetSliderPosition)
{
  if (newTargetSliderPosition != m_lastTargetSlidePosition)
  {
    m_slideStepper->moveTo(newTargetSliderPosition);

    // Remember new target slide position
    Serial.printf("New Slide Position Target: %d -> %d\n", m_lastTargetSlidePosition, newTargetSliderPosition);
    m_lastTargetSlidePosition= newTargetSliderPosition;
    setIsMovingToTargetFlag(true);

    // Tell any clients what the final target slider position is
    m_listener->onSliderTargetSet(newTargetSliderPosition);    
  }
}

float computeSecondsToCompleteMove(FastAccelStepper* stepper, const int32_t absSteps)
{
  const float stepVel= (float)stepper->getSpeedInMilliHz() / 1000.f;
  const float stepAccel= (float)stepper->getAcceleration();
  Serial.printf("computeSecondsToCompleteMove\n");  
  Serial.printf(" stepVel: %f, stepAccel: %f\n", stepVel, stepAccel);

  const float timeToFullSpeed = stepVel / stepAccel; // v = a*t -> t = v_target / a
  Serial.printf(" timeToFullSpeed: %f\n", timeToFullSpeed);
  const float maxAllowedRampSteps = 0.5f*(float)absSteps; // Need to ramp up and down in absSteps
  const float clampedStepsToFullSpeed = min(0.5f * stepAccel * timeToFullSpeed * timeToFullSpeed, maxAllowedRampSteps); // s = 0.5*a*t^2
  Serial.printf(" clampedStepsToFullSpeed: %f\n", clampedStepsToFullSpeed);
  const float clampedTimeToFullSpeed= sqrt(2.f* clampedStepsToFullSpeed / stepAccel); // t = sqrt(2*s/a)
  Serial.printf(" clampedTimeToFullSpeed: %f\n", clampedTimeToFullSpeed);

  const float stepsAtFullSpeed = max((float)absSteps - 2.f * clampedStepsToFullSpeed, 0.f);
  const float timeAtFullSpeed = (float)stepsAtFullSpeed / stepVel;
  Serial.printf(" stepsAtFullSpeed: %f\n", stepsAtFullSpeed);
  Serial.printf(" timeAtFullSpeed: %f\n", timeAtFullSpeed);

  const float totalMoveTime = 2.f*clampedTimeToFullSpeed + timeAtFullSpeed;
  Serial.printf(" totalMoveTime: %f\n", totalMoveTime);

  return totalMoveTime;  
}

void setStepperSpeedAndAccelByStepsAndTime(FastAccelStepper* stepper, int32_t absSteps, float timeToComplete)
{
  const float currentStepAccel= (float)stepper->getAcceleration();
  // Position due to acceleration only after time is: s = a*t^2 
  // Solving for acceleration: a = s / t^2
  // Min acceleration is the acceleration that takes us to the halfway point
  // so a_min = s / t^2 = (absSteps / 2) / (timeToComplete / 2)^2 = 4 * absSteps / timeToComplete^2
  const float minStepAccel= 4.f * absSteps / (timeToComplete * timeToComplete);

  float targetAccel;
  float timeToTargetVel;
  if (currentStepAccel > minStepAccel)
  {
    // Solve for time to accelerate to target velocity given total steps S, acceleration a, and total time T
    // S = S0 + S1 + S2, 
    // where S0 = S2 = distance covered ramping up/down to/from target velocity and S1 is distance covered at target vel
    // T = t0 + t1 + t2
    // where t0 = t2 = time spent ramping up/down to/from target velocity and t1 is time spent at target vel
    // S0 = S2 = 0.5*a*t0^2
    // S1 = Vt*t1, where Vt is the target velocity we want to solve for
    // Solving for t1 we get
    // t1 = T - t0 - t2 = T - 2*t0
    // Plugging this all in for S0, S1, and S2 we get 
    // S = 2*(0.5*a*t0^2) + Vt*(T - 2*t0)
    // Which can be written in standard quadratic form
    // 0 = -t0^2 + T*t0 - S/a
    // And then solving for t0 using quadratic formula
    // t0 = 0.5*(T +/- sqrt(T^2 - 4*S/a))
    // There are two solutions: a short time/fast speed (pos root) and a long time/slow speed (neg root)
    // we always choose the long time/slow speed solution
    // Note that the sqrt becomes imaginary(invalid) if we use an acceleration < minStepAccel,
    // which means the acceleration isn't fast enough to get to symmetrically ramp back down for the desired absSteps and timeToComplete
    timeToTargetVel= 0.5f * (timeToComplete - sqrt(max(timeToComplete*timeToComplete - 4.f*(absSteps/currentStepAccel), 0.f)));
    targetAccel= currentStepAccel;    
  }
  else
  {
    timeToTargetVel = 0.5f * timeToComplete;
    targetAccel= minStepAccel;
  }

  float targetVel= targetAccel * timeToTargetVel; // Compute velocity at halfway time using min acceleration

  stepper->setAcceleration((int32_t)targetAccel);
  stepper->setSpeedInHz((uint32_t)targetVel);  
}

void SliderState::setSlidePanTiltPosFraction(float slideFraction, float panFraction, float tiltFraction)
{
  // Re-apply global speed and acceleration fractions to each slider
  applyLastSpeedFraction();
  applyLastAccelFraction();

  // Remap [-1, 1] unit position to calibrated slider min and max step position  
  int32_t newSlidePosition= remapFloatToInt32(-1.f, 1.f, m_sliderStepperMin, m_sliderStepperMax, slideFraction);

  float newTargetPanDegrees= remapFloatToFloat(-1.f, 1.f, PAN_MIN_ANGLE, PAN_MAX_ANGLE, panFraction);
  // Use Pan gear ratio to determine motor target angle from given camera angle
  float clampedCameraPanDegrees = max(min(newTargetPanDegrees, PAN_MAX_ANGLE), PAN_MIN_ANGLE);
  float motorPanDegrees = clampedCameraPanDegrees * PAN_GEAR_RATIO;
  // degrees*steps/degree + center step position = target step position
  int32_t newPanPosition= motorAngleToSteps(motorPanDegrees) + m_panStepperCenter;
  
  float newTargetTiltDegrees= remapFloatToFloat(-1.f, 1.f, TILT_MIN_ANGLE, TILT_MAX_ANGLE, tiltFraction);  
  // Use Tilt gear ratio to determine motor target angle from given camera angle
  float clampedCameraTiltDegrees = max(min(newTargetTiltDegrees, TILT_MAX_ANGLE), TILT_MIN_ANGLE);
  float motorTiltDegrees = clampedCameraTiltDegrees * TILT_GEAR_RATIO;
  // degrees*steps/degree + center step position = target step position
  int32_t newTiltPosition= motorAngleToSteps(motorTiltDegrees) + m_tiltStepperCenter;

  Serial.printf("New SlidePanTilt Fractions: %f, %f, %d\n", slideFraction, panFraction, tiltFraction);
  Serial.printf("    SlidePanTilt Positions: %d, %d, %d\n", newSlidePosition, newPanPosition, newTiltPosition);

  // See if any slider actually wants to move
  if (m_lastTargetSlidePosition != newSlidePosition || 
      m_lastTargetPanPosition != newPanPosition ||
      m_lastTargetTiltPosition != newTiltPosition)
  {
    // Compute the absolute number of steps each slider has to move
    const int32_t absSlideSteps= abs(m_lastTargetSlidePosition - newSlidePosition);
    const int32_t absPanSteps= abs(m_lastTargetPanPosition - newPanPosition);
    const int32_t absTiltSteps= abs(m_lastTargetTiltPosition - newTiltPosition);

    Serial.printf("    SlidePanTilt Steps: %d, %d, %d\n", absSlideSteps, absPanSteps, absTiltSteps);

    // Find the max time amongst all moving steppers that it will take to reach their target step position
    float maxTimeToComplete= 0;
    if (absSlideSteps > 0)
    {
      maxTimeToComplete= max(computeSecondsToCompleteMove(m_slideStepper, absSlideSteps), maxTimeToComplete);
    }
    if (absPanSteps > 0)
    {
      maxTimeToComplete= max(computeSecondsToCompleteMove(m_panStepper, absPanSteps), maxTimeToComplete);
    }
    if (absTiltSteps > 0)
    {
      maxTimeToComplete= max(computeSecondsToCompleteMove(m_tiltStepper, absTiltSteps), maxTimeToComplete);
    }

    Serial.printf("    Time to complete: %f\n", maxTimeToComplete);

    // Given a desired time to complete, recompute each slider speed so that they all complete at the same time
    if (maxTimeToComplete > 0.f)
    {
      if (absSlideSteps > 0)
      {
        setStepperSpeedAndAccelByStepsAndTime(m_slideStepper, absSlideSteps, maxTimeToComplete);
      }
      if (absPanSteps > 0)
      {
        setStepperSpeedAndAccelByStepsAndTime(m_panStepper, absPanSteps, maxTimeToComplete);
      }
      if (absTiltSteps > 0)
      {
        setStepperSpeedAndAccelByStepsAndTime(m_tiltStepper, absTiltSteps, maxTimeToComplete);
      }
    }

    setSliderPosFraction(slideFraction);
    setPanPosFraction(panFraction);
    setTiltPosFraction(tiltFraction);
  }
}

void SliderState::setSliderPosFraction(float fraction)
{
  // Remap [-1, 1] unit position to calibrated slider min and max step position  
  int32_t newTargetSliderPosition= remapFloatToInt32(-1.f, 1.f, m_sliderStepperMin, m_sliderStepperMax, fraction);

  setSlideStepperPosition(newTargetSliderPosition);
}

float SliderState::getSliderPosFraction()
{
  return remapInt32ToFloat(m_sliderStepperMin, m_sliderStepperMax, -1.f, 1.f, getSlideStepperPosition());
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

void SliderState::setTiltPosFraction(float fraction)
{
  float newTargetDegrees= remapFloatToFloat(-1.f, 1.f, TILT_MIN_ANGLE, TILT_MAX_ANGLE, fraction);
  setTiltStepperTargetDegrees(newTargetDegrees);
}

float SliderState::getTiltPosFraction()
{
  return remapFloatToFloat(TILT_MIN_ANGLE, TILT_MAX_ANGLE, -1.f, 1.f, getTiltStepperDegrees());
}

void SliderState::setSpeedFraction(float newFraction)
{
  if (newFraction != m_lastSpeedFraction)
  {
    Serial.printf("New Speed Target: %f -> %f\n", m_lastSpeedFraction, newFraction);
    m_lastSpeedFraction= newFraction;
    applyLastSpeedFraction();
  }
}

void SliderState::applyLastSpeedFraction()
{
  // (special case for 0: use 0 speed to stop the stepper rather than ..._MIN_SPEED)
  // Remap [0, 1] unit speed to mm/s
  float newTargetSliderSpeed= m_lastSpeedFraction > 0.f ? remapFloatToFloat(0.f, 1.f, SLIDE_MIN_SPEED, SLIDE_MAX_SPEED, m_lastSpeedFraction) : 0.f;
  // Remap [0, 1] unit speed to deg/s
  float newTargetPanSpeed= m_lastSpeedFraction > 0.f ? remapFloatToFloat(0.f, 1.f, PAN_MIN_SPEED, PAN_MAX_SPEED, m_lastSpeedFraction) : 0.f;
  float newTargetTiltSpeed= m_lastSpeedFraction > 0.f ? remapFloatToFloat(0.f, 1.f, TILT_MIN_SPEED, TILT_MAX_SPEED, m_lastSpeedFraction) : 0.f;

  setSlideStepperLinearSpeed(newTargetSliderSpeed);
  setPanStepperAngularSpeed(newTargetPanSpeed);
  setTiltStepperAngularSpeed(newTargetTiltSpeed);
}

void SliderState::applyLastAccelFraction()
{
  // Remap [0, 1] unit accel to mm/s^2
  float newTargetSliderAccel= remapFloatToFloat(0.f, 1.f, SLIDE_MIN_ACCELERATION, SLIDE_MAX_ACCELERATION, m_lastAccelerationFraction);
  // Remap [0, 1] unit accel to deg/s^2
  float newTargetPanAccel= remapFloatToFloat(0.f, 1.f, PAN_MIN_ACCELERATION, PAN_MAX_ACCELERATION, m_lastAccelerationFraction);
  float newTargetTiltAccel= remapFloatToFloat(0.f, 1.f, TILT_MIN_ACCELERATION, TILT_MAX_ACCELERATION, m_lastAccelerationFraction);

  setSlideStepperLinearAcceleration(newTargetSliderAccel);
  setPanStepperAngularAcceleration(newTargetPanAccel);
  setTiltStepperAngularAcceleration(newTargetTiltAccel);
}

float SliderState::getSpeedFraction()
{
  return m_lastSpeedFraction;
}

void SliderState::setAccelFraction(float newFraction)
{
  if (newFraction != m_lastAccelerationFraction)
  {
    // Remap [0, 1] unit accel to mm/s^2
    float newTargetSliderAccel= remapFloatToFloat(0.f, 1.f, SLIDE_MIN_ACCELERATION, SLIDE_MAX_ACCELERATION, newFraction);
    // Remap [0, 1] unit accel to deg/s^2
    float newTargetPanAccel= remapFloatToFloat(0.f, 1.f, PAN_MIN_ACCELERATION, PAN_MAX_ACCELERATION, newFraction);
    float newTargetTiltAccel= remapFloatToFloat(0.f, 1.f, TILT_MIN_ACCELERATION, TILT_MAX_ACCELERATION, newFraction);

    setSlideStepperLinearAcceleration(newTargetSliderAccel);
    setPanStepperAngularAcceleration(newTargetPanAccel);
    setTiltStepperAngularAcceleration(newTargetTiltAccel);

    Serial.printf("New Acceleration Target: %f -> %f\n", m_lastAccelerationFraction, newFraction);
    m_lastAccelerationFraction= newFraction;
  }
}

float SliderState::getAccelFraction()
{
  return m_lastAccelerationFraction;
}