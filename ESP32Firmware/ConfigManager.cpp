#include "ConfigManager.h"
#include <WiFi.h>

#define PREFS_NAMESPACE     "camera-control"

ConfigManager* ConfigManager::s_instance= nullptr;

ConfigManager::ConfigManager()
  : m_preferences()
  , m_ip(127, 0, 0, 1)
  , m_gateway(127, 0, 0, 1)
  , m_subnet(255, 255, 255, 0)
{
  memset(m_ssid, 0, sizeof(m_ssid));
  memset(m_password, 0, sizeof(m_password));

  s_instance= this;
}

void ConfigManager::load()
{
  if (m_preferences.begin(PREFS_NAMESPACE, false))
  {
    size_t bytesRead= 0;

    m_preferences.getString("ssid", m_ssid, MAX_SSID_LENGTH);
    m_preferences.getString("password", m_password, MAX_PASSWORD_LENGTH);
    m_preferences.getBytes("ip", &m_ip, sizeof(IPAddress));
    m_preferences.getBytes("gateway", &m_gateway, sizeof(IPAddress));
    m_preferences.getBytes("subnet", &m_subnet, sizeof(IPAddress));

    Serial.printf("[Wifi Config]\n");
    Serial.printf("  ssid: %s\n", m_ssid);
    Serial.printf("  password: %s\n", m_password);
    Serial.printf("  ip: %s\n", m_ip.toString().c_str());
    Serial.printf("  gateway: %s\n", m_gateway.toString().c_str());
    Serial.printf("  subnet: %s\n", m_subnet.toString().c_str());

    Serial.printf("[Motor Calibration Config]\n");
    bytesRead= m_preferences.getBytes("motor_cal", &m_motorCalibrationConfig, sizeof(StepperMotorCalibration));
    if (bytesRead != sizeof(StepperMotorCalibration) || m_motorCalibrationConfig.version != STEPPER_MOTOR_CALIBRATION_VER)
    {
      Serial.printf("  Invalid. Setting Defaults.\n");
      memset(&m_motorCalibrationConfig, 0, sizeof(StepperMotorCalibration));
    }
    Serial.printf("  ver: %d\n", m_motorCalibrationConfig.version);
    Serial.printf("  pan_center: %d\n", m_motorCalibrationConfig.panStepperCenter);
    Serial.printf("  tilt_center: %d\n", m_motorCalibrationConfig.tiltStepperCenter);
    Serial.printf("  slide_min: %d\n", m_motorCalibrationConfig.slideStepperMin);
    Serial.printf("  slide_max: %d\n", m_motorCalibrationConfig.slideStepperMax);

    Serial.printf("[Motor Position Config]\n");
    bytesRead= m_preferences.getBytes("motor_pos", &m_motorPositionConfig, sizeof(StepperMotorPosition));
    if (bytesRead != sizeof(StepperMotorPosition) || m_motorPositionConfig.version != STEPPER_MOTOR_POSITION_VER)
    {
      Serial.printf("  Invalid. Setting Defaults.\n");
      memset(&m_motorPositionConfig, 0, sizeof(StepperMotorPosition));
      m_motorPositionConfig.version= STEPPER_MOTOR_POSITION_VER;
    }
    Serial.printf("  ver: %d\n", m_motorPositionConfig.version);
    Serial.printf("  pan_position: %d\n", m_motorPositionConfig.panStepperPosition);
    Serial.printf("  tilt_position: %d\n", m_motorPositionConfig.tiltStepperPosition);
    Serial.printf("  slide_position: %d\n", m_motorPositionConfig.slideStepperPosition);
    
    m_isMotorPositionConfigDirty= false;
    m_bValidPrefs= true;
  }
  else
  {
    Serial.printf("Failed to load config");
  }
}

void ConfigManager::setListener(ConfigEventListener *listener)
{
  m_listener= listener; 
}

void ConfigManager::clearListener(ConfigEventListener *listener)
{
  if (m_listener == listener)
    m_listener= nullptr;
}

void ConfigManager::setSSID(const char* newValue)
{
  strncpy(m_ssid, newValue, MAX_SSID_LENGTH);
  if (m_bValidPrefs)
    m_preferences.putString("ssid", m_ssid);  
  if (m_listener != nullptr)
    m_listener->onSSIDChanged();
}

void ConfigManager::setPassword(const char* newValue)
{
  strncpy(m_password, newValue, MAX_PASSWORD_LENGTH);
  if (m_bValidPrefs)
    m_preferences.putString("password", m_password);
  if (m_listener != nullptr)
    m_listener->onPasswordChanged();    
}

void ConfigManager::setIPAddress(const IPAddress& newValue)
{
  m_ip= newValue;
  if (m_bValidPrefs)
    m_preferences.putBytes("ip", &m_ip, sizeof(IPAddress));
}

void ConfigManager::setGateway(const IPAddress& newValue)
{
  m_gateway= newValue;
  if (m_bValidPrefs)
    m_preferences.putBytes("gateway", &m_gateway, sizeof(IPAddress));
}

void ConfigManager::setSubnet(const IPAddress& newValue)
{
  m_subnet= newValue;
  if (m_bValidPrefs)
    m_preferences.putBytes("subnet", &m_subnet, sizeof(IPAddress));
}

bool ConfigManager::getMotorCalibrationConfig(StepperMotorCalibration& outMotorConfig) const
{
  outMotorConfig= m_motorCalibrationConfig;
  return outMotorConfig.version == STEPPER_MOTOR_CALIBRATION_VER;
}

void ConfigManager::setMotorCalibrationConfig(const StepperMotorCalibration& motorConfig)
{
  m_motorCalibrationConfig= motorConfig;
  m_motorCalibrationConfig.version= STEPPER_MOTOR_CALIBRATION_VER;
  if (m_bValidPrefs)
    m_preferences.putBytes("motor_cal", &m_motorCalibrationConfig, sizeof(StepperMotorCalibration));  
}

bool ConfigManager::getMotorPositionConfig(StepperMotorPosition& outMotorConfig) const
{
  outMotorConfig= m_motorPositionConfig;
  return outMotorConfig.version == STEPPER_MOTOR_POSITION_VER;
}

void ConfigManager::setMotorPanPosition(int32_t panStepperPosition)
{
  if (m_motorPositionConfig.panStepperPosition != panStepperPosition)
  {
    m_motorPositionConfig.panStepperPosition= panStepperPosition;
    m_isMotorPositionConfigDirty= true;
  }
}

void ConfigManager::setMotorTiltPosition(int32_t tiltStepperPosition)
{
  if (m_motorPositionConfig.tiltStepperPosition != tiltStepperPosition)
  {
    m_motorPositionConfig.tiltStepperPosition= tiltStepperPosition;
    m_isMotorPositionConfigDirty= true;
  }
}

void ConfigManager::setMotorSlidePosition(int32_t slideStepperPosition)
{
  if (m_motorPositionConfig.slideStepperPosition != slideStepperPosition)
  {
    m_motorPositionConfig.slideStepperPosition= slideStepperPosition;
    m_isMotorPositionConfigDirty= true;
  }
}

bool ConfigManager::saveMotorPositionConfig()
{
  if (m_bValidPrefs && m_isMotorPositionConfigDirty)
  {
    Serial.printf("Motor Position Config Auto-saved.\n");
    size_t bytesWritten= m_preferences.putBytes("motor_pos", &m_motorPositionConfig, sizeof(StepperMotorPosition));
    if (bytesWritten == sizeof(StepperMotorPosition))
    {
      m_isMotorPositionConfigDirty= false;
      Serial.printf("  ver: %d\n", m_motorPositionConfig.version);
      Serial.printf("  pan_position: %d\n", m_motorPositionConfig.panStepperPosition);
      Serial.printf("  tilt_position: %d\n", m_motorPositionConfig.tiltStepperPosition);
      Serial.printf("  slide_position: %d\n", m_motorPositionConfig.slideStepperPosition);
      return true;
    }
    else
    {
      Serial.printf("  Failed to write motor position config!\n");
      return false;
    }
  }
  else
  {
    Serial.printf("Motor Position Config not dirty. Skipping auto-save.\n");
    return false;
  }
}