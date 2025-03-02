#ifndef CONFIG_MANAGER_H__
#define CONFIG_MANAGER_H__

#include <Arduino.h>
#include "Preferences.h"

#define MAX_EVENT_LISTENERS     4
#define STEPPER_MOTOR_CALIBRATION_VER   1
#define STEPPER_MOTOR_POSITION_VER      1
#define STEPPER_MOTOR_LIMITS_VER        1

#define DEFAULT_PAN_MIN_ANGLE -180.f // degrees
#define DEFAULT_PAN_MAX_ANGLE 180.f // degrees
#define DEFAULT_PAN_MIN_SPEED 5.f // degrees / s
#define DEFAULT_PAN_MAX_SPEED 250.f // degrees / s
#define DEFAULT_PAN_MIN_ACCELERATION 100.f // degrees / s²
#define DEFAULT_PAN_MAX_ACCELERATION 1500.f // degrees / s²

#define DEFAULT_TILT_MIN_ANGLE -60.f // degrees
#define DEFAULT_TILT_MAX_ANGLE 45.f // degrees
#define DEFAULT_TILT_MIN_SPEED 5.f // degrees / s
#define DEFAULT_TILT_MAX_SPEED 50.f // degrees / s
#define DEFAULT_TILT_MIN_ACCELERATION 50.f // degrees / s²
#define DEFAULT_TILT_MAX_ACCELERATION 3500.f // degrees / s²

#define DEFAULT_SLIDE_MIN_SPEED 5.f // mm / s
#define DEFAULT_SLIDE_MAX_SPEED 300.f // mm / s
#define DEFAULT_SLIDE_MIN_ACCELERATION 10.f // mm / s²
#define DEFAULT_SLIDE_MAX_ACCELERATION 350.f // mm / s²

class ConfigEventListener
{
public:
  virtual void onLimitsChanged() {}
};

struct StepperMotorLimits
{
  int32_t version;
  
  float panMinAngle; // degrees
  float panMaxAngle; // degrees
  float panMinSpeed; // degrees / s
  float panMaxSpeed; // degrees / s
  float panMinAcceleration; // degrees / s²
  float panMaxAcceleration; // degrees / s²

  float tiltMinAngle; // degrees
  float tiltMaxAngle; // degrees
  float tiltMinSpeed; // degrees / s
  float tiltMaxSpeed; // degrees / s
  float tiltMinAcceleration; // degrees / s²
  float tiltMaxAcceleration; // degrees / s²

  float slideMinSpeed; // mm / s
  float slideMaxSpeed; // mm / s
  float slideMinAcceleration; // mm / s²
  float slideMaxAcceleration; // mm / s²

  void setDefaults()
  {
    panMinAngle= DEFAULT_PAN_MIN_ANGLE;
    panMaxAngle= DEFAULT_PAN_MAX_ANGLE;
    panMinSpeed= DEFAULT_PAN_MIN_SPEED;
    panMaxSpeed= DEFAULT_PAN_MAX_SPEED;
    panMinAcceleration= DEFAULT_PAN_MIN_ACCELERATION;
    panMaxAcceleration= DEFAULT_PAN_MAX_ACCELERATION;

    tiltMinAngle= DEFAULT_TILT_MIN_ANGLE;
    tiltMaxAngle= DEFAULT_TILT_MAX_ANGLE;
    tiltMinSpeed= DEFAULT_TILT_MIN_SPEED;
    tiltMaxSpeed= DEFAULT_TILT_MAX_SPEED;
    tiltMinAcceleration= DEFAULT_TILT_MIN_ACCELERATION;
    tiltMaxAcceleration= DEFAULT_TILT_MAX_ACCELERATION;

    slideMinSpeed= DEFAULT_SLIDE_MIN_SPEED;
    slideMaxSpeed= DEFAULT_SLIDE_MAX_SPEED;
    slideMinAcceleration= DEFAULT_SLIDE_MIN_ACCELERATION;
    slideMaxAcceleration= DEFAULT_SLIDE_MAX_ACCELERATION;
  }
};

struct StepperMotorCalibration
{
  int32_t version;
  int32_t panStepperCenter;
  int32_t tiltStepperCenter;
  int32_t slideStepperMin;
  int32_t slideStepperMax;  
};

struct StepperMotorPosition
{
  int32_t version;
  int32_t panStepperPosition;
  int32_t tiltStepperPosition;
  int32_t slideStepperPosition;
};

class ConfigManager
{
public:
  ConfigManager();

  static ConfigManager* getInstance() { return s_instance; }

  void load();

  void setListener(ConfigEventListener *listener);
  void clearListener(ConfigEventListener *listener);

  void resetMotorLimits();
  void getMotorLimitsConfig(StepperMotorLimits& outMotorConfig) const;
  void setMotorLimitsConfig(const StepperMotorLimits& motorConfig);
  void notifyMotorLimitsChanged();

  void setPanMotorLimits(float minAngle, float maxAngle, float minSpeed, float maxSpeed, float minAccel, float maxAccel);
  void setTiltMotorLimits(float minAngle, float maxAngle, float minSpeed, float maxSpeed, float minAccel, float maxAccel);
  void setSlideMotorLimits(float minSpeed, float maxSpeed, float minAccel, float maxAccel);

  bool getMotorCalibrationConfig(StepperMotorCalibration& outMotorConfig) const;
  void setMotorCalibrationConfig(const StepperMotorCalibration& motorConfig);

  bool getMotorPositionConfig(StepperMotorPosition& outMotorConfig) const;
  void setMotorPanPosition(int32_t panStepperPosition);
  void setMotorTiltPosition(int32_t tiltStepperPosition);
  void setMotorSlidePosition(int32_t slideStepperPosition);
  bool saveMotorPositionConfig();

private:
  static ConfigManager* s_instance;

  Preferences m_preferences;
  bool m_bValidPrefs= false;

  // Listener
  ConfigEventListener* m_listener;

  // Slider Manager Config
  StepperMotorLimits m_motorLimitsConfig;
  StepperMotorCalibration m_motorCalibrationConfig;
  StepperMotorPosition m_motorPositionConfig;
  bool m_isMotorPositionConfigDirty= false;
};

#endif