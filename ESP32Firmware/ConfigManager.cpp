#include "ConfigManager.h"
#include <WiFi.h>

#define PREFS_NAMESPACE     "camera-control"

ConfigManager* ConfigManager::s_instance= nullptr;

ConfigManager::ConfigManager()
  : m_preferences()
{
  s_instance= this;
}

void ConfigManager::load()
{
  if (m_preferences.begin(PREFS_NAMESPACE, false))
  {
    size_t bytesRead= 0;

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