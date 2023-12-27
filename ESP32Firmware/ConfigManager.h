#ifndef CONFIG_MANAGER_H__
#define CONFIG_MANAGER_H__

#include <Arduino.h>
#include "Preferences.h"

#define MAX_SSID_LENGTH         32
#define MAX_PASSWORD_LENGTH     63
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

  inline const char* getSSID() const { return m_ssid; }
  inline bool hasSSID() const { return m_ssid[0] != '\0'; }
  inline const char* getPassword() const { return m_password; }
  inline bool hasPassword() const { return m_password[0] != '\0'; }
  inline const IPAddress& getIPAddress() const { return m_ip; }
  inline const IPAddress& getGateway() const { return m_gateway; }
  inline const IPAddress& getSubnet() const { return m_subnet; }

  void setSSID(const char* newValue);
  void setPassword(const char* newValue);
  void setIPAddress(const IPAddress& newValue);
  void setGateway(const IPAddress& newValue);
  void setSubnet(const IPAddress& newValue);

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

  // WiFi stuff
  char m_ssid[MAX_SSID_LENGTH+1];
  char m_password[MAX_PASSWORD_LENGTH+1];
  IPAddress m_ip;
  IPAddress m_gateway;
  IPAddress m_subnet;  

  // Slider Manager Config
  StepperMotorCalibration m_motorCalibrationConfig;
  StepperMotorPosition m_motorPositionConfig;
  bool m_isMotorPositionConfigDirty= false;
};

#endif