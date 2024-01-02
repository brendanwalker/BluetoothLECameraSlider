#ifndef CONFIG_MANAGER_H__
#define CONFIG_MANAGER_H__

#include <Arduino.h>
#include "Preferences.h"

#define MAX_EVENT_LISTENERS     4
#define STEPPER_MOTOR_CALIBRATION_VER   1
#define STEPPER_MOTOR_POSITION_VER      1

class ConfigEventListener
{
public:
  virtual void onSSIDChanged() {}
  virtual void onPasswordChanged() {}
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
  StepperMotorCalibration m_motorCalibrationConfig;
  StepperMotorPosition m_motorPositionConfig;
  bool m_isMotorPositionConfigDirty= false;
};

#endif