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

    Serial.printf("[Motor Limits Config]\n");
    bytesRead= m_preferences.getBytes("motor_limits", &m_motorLimitsConfig, sizeof(StepperMotorLimits));
    if (bytesRead != sizeof(StepperMotorLimits) || m_motorLimitsConfig.version != STEPPER_MOTOR_LIMITS_VER)
    {
      Serial.printf("  Invalid. Setting Defaults.\n");
      m_motorLimitsConfig.setDefaults();
      m_motorLimitsConfig.version= STEPPER_MOTOR_LIMITS_VER;
    }
    Serial.printf("  ver: %d\n", m_motorLimitsConfig.version);
    Serial.printf("  panMinAngle: %.1f degrees\n", m_motorLimitsConfig.panMinAngle);
    Serial.printf("  panMaxAngle: %.1f degrees\n", m_motorLimitsConfig.panMaxAngle);
    Serial.printf("  panMinSpeed: %.1f degrees / s\n", m_motorLimitsConfig.panMinSpeed);
    Serial.printf("  panMaxSpeed: %.1f degrees / s\n", m_motorLimitsConfig.panMaxSpeed);
    Serial.printf("  panMinAcceleration: %.1f degrees / s²\n", m_motorLimitsConfig.panMinAcceleration);
    Serial.printf("  panMaxAcceleration: %.1fdegrees / s²\n", m_motorLimitsConfig.panMaxAcceleration);
    Serial.printf("  tiltMinAngle: %.1f degrees\n", m_motorLimitsConfig.tiltMinAngle);
    Serial.printf("  tiltMaxAngle: %.1f degrees\n", m_motorLimitsConfig.tiltMaxAngle);
    Serial.printf("  tiltMinSpeed: %.1f degrees / s\n", m_motorLimitsConfig.tiltMinSpeed);
    Serial.printf("  tiltMaxSpeed: %.1f degrees / s\n", m_motorLimitsConfig.tiltMaxSpeed);
    Serial.printf("  tiltMinAcceleration: %.1f degrees / s²\n", m_motorLimitsConfig.tiltMinAcceleration);
    Serial.printf("  tiltMaxAcceleration: %.1f degrees / s²\n", m_motorLimitsConfig.tiltMaxAcceleration);
    Serial.printf("  slideMinSpeed: %.1f mm / s\n", m_motorLimitsConfig.slideMinSpeed);
    Serial.printf("  slideMaxSpeed: %.1f mm / s\n", m_motorLimitsConfig.slideMaxSpeed);
    Serial.printf("  slideMinAcceleration: %.1f mm / s²\n", m_motorLimitsConfig.slideMinAcceleration);
    Serial.printf("  slideMaxAcceleration: %.1f mm / s²\n", m_motorLimitsConfig.slideMaxAcceleration);

    Serial.printf("[Motor Calibration Config]\n");
    bytesRead= m_preferences.getBytes("motor_cal", &m_motorCalibrationConfig, sizeof(StepperMotorCalibration));
    if (bytesRead != sizeof(StepperMotorCalibration) || m_motorCalibrationConfig.version != STEPPER_MOTOR_CALIBRATION_VER)
    {
      Serial.printf("  Invalid. Setting Defaults.\n");
      memset(&m_motorCalibrationConfig, 0, sizeof(StepperMotorCalibration));
      m_motorCalibrationConfig.version= STEPPER_MOTOR_CALIBRATION_VER;
    }
    Serial.printf("  ver: %d\n", m_motorCalibrationConfig.version);
    Serial.printf("  panStepperCenter: %d\n", m_motorCalibrationConfig.panStepperCenter);
    Serial.printf("  tiltStepperCenter: %d\n", m_motorCalibrationConfig.tiltStepperCenter);
    Serial.printf("  slideStepperMin: %d\n", m_motorCalibrationConfig.slideStepperMin);
    Serial.printf("  slideStepperMax: %d\n", m_motorCalibrationConfig.slideStepperMax);

    Serial.printf("[Motor Position Config]\n");
    bytesRead= m_preferences.getBytes("motor_pos", &m_motorPositionConfig, sizeof(StepperMotorPosition));
    if (bytesRead != sizeof(StepperMotorPosition) || m_motorPositionConfig.version != STEPPER_MOTOR_POSITION_VER)
    {
      Serial.printf("  Invalid. Setting Defaults.\n");
      memset(&m_motorPositionConfig, 0, sizeof(StepperMotorPosition));
      m_motorPositionConfig.version= STEPPER_MOTOR_POSITION_VER;
    }
    Serial.printf("  ver: %d\n", m_motorPositionConfig.version);
    Serial.printf("  panStepperPosition: %d\n", m_motorPositionConfig.panStepperPosition);
    Serial.printf("  tiltStepperPosition: %d\n", m_motorPositionConfig.tiltStepperPosition);
    Serial.printf("  slideStepperPosition: %d\n", m_motorPositionConfig.slideStepperPosition);
    
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

void ConfigManager::getMotorLimitsConfig(StepperMotorLimits& outMotorConfig) const
{
  if (m_motorLimitsConfig.version == STEPPER_MOTOR_LIMITS_VER)
  {
    outMotorConfig= m_motorLimitsConfig;
  }
  else
  {
    outMotorConfig.setDefaults();
  }
}

void ConfigManager::setMotorLimitsConfig(const StepperMotorLimits& motorConfig)
{
  m_motorLimitsConfig= motorConfig;
  m_motorLimitsConfig.version= STEPPER_MOTOR_LIMITS_VER;
  notifyMotorLimitsChanged();
}

void ConfigManager::notifyMotorLimitsChanged()
{
  if (m_bValidPrefs)
    m_preferences.putBytes("motor_limits", &m_motorLimitsConfig, sizeof(StepperMotorLimits));
  if (m_listener != nullptr)
    m_listener->onLimitsChanged();
}

void ConfigManager::resetMotorLimits()
{
  m_motorLimitsConfig.setDefaults();
  notifyMotorLimitsChanged();
}

void ConfigManager::setPanMotorLimits(float minAngle, float maxAngle, float minSpeed, float maxSpeed, float minAccel, float maxAccel)
{
  m_motorLimitsConfig.panMinAngle= minAngle;
  m_motorLimitsConfig.panMaxAngle= maxAngle;
  m_motorLimitsConfig.panMinSpeed= minSpeed;
  m_motorLimitsConfig.panMaxSpeed= maxSpeed;
  m_motorLimitsConfig.panMinAcceleration= minAccel;
  m_motorLimitsConfig.panMaxAcceleration= maxAccel;
  notifyMotorLimitsChanged();
}

void ConfigManager::setTiltMotorLimits(float minAngle, float maxAngle, float minSpeed, float maxSpeed, float minAccel, float maxAccel)
{
  m_motorLimitsConfig.tiltMinAngle= minAngle;
  m_motorLimitsConfig.tiltMaxAngle= maxAngle;
  m_motorLimitsConfig.tiltMinSpeed= minSpeed;
  m_motorLimitsConfig.tiltMaxSpeed= maxSpeed;
  m_motorLimitsConfig.tiltMinAcceleration= minAccel;
  m_motorLimitsConfig.tiltMaxAcceleration= maxAccel;
  notifyMotorLimitsChanged();
}

void ConfigManager::setSlideMotorLimits(float minSpeed, float maxSpeed, float minAccel, float maxAccel)
{
  m_motorLimitsConfig.slideMinSpeed= minSpeed;
  m_motorLimitsConfig.slideMaxSpeed= maxSpeed;
  m_motorLimitsConfig.slideMinAcceleration= minAccel;
  m_motorLimitsConfig.slideMaxAcceleration= maxAccel;
  notifyMotorLimitsChanged();
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