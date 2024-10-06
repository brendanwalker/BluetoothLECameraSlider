#include "FanManager.h"
#include "SliderManager.h"

#define POWER_BLEND         0.1f
#define MAX_POWER_VALUE     1023.f
#define MIN_POWER_VALUE     512.f
#define MOTOR_COOLDOWN      5.f

FanManager* FanManager::s_instance = nullptr;

FanManager::FanManager(uint8_t fanPin)
    : m_fanPin(fanPin)
    , m_fanPowerFraction(0.f)
{
    s_instance= this;

    pinMode(m_fanPin, OUTPUT);
}

void FanManager::setup()
{       
    analogWrite(m_fanPin, 0);
}

void FanManager::engageFan()
{
    if (m_fanActiveTimer <= 0.f)
    {
        Serial.printf("Fan Cooldown Reset\n");
    }

    m_fanActiveTimer= MOTOR_COOLDOWN;
} 

void FanManager::updateFanTimer(float deltaSeconds)
{
    // Count down any active cooldown timer
    m_fanActiveTimer = max(m_fanActiveTimer - deltaSeconds, 0.f);

    // Restart cooldown timer if any motor is running
    if (SliderState::getInstance()->isAnySliderRunning())
    {
        engageFan();
    }     
}

void FanManager::updateFanPowerFraction()
{
    // Blend the motor power up or down depending on wether we want the fan active
    float targetFraction= (m_fanActiveTimer > 0.f) ? 1.f : 0.f;
    m_fanPowerFraction= (1.f - POWER_BLEND)*m_fanPowerFraction + POWER_BLEND*targetFraction;

    // Fan power fraction does ever seem to completely blend to 0
    if (m_fanPowerFraction < 0.01f && targetFraction <= 0.f)
    {
        m_fanPowerFraction= 0.f;        
    }
}

void FanManager::applyPowerFraction()
{
    int newAnalogValue= 0;
    if (m_fanPowerFraction > 0.f)
    {
        newAnalogValue= (int)((1.f - m_fanPowerFraction)*MIN_POWER_VALUE + m_fanPowerFraction*MAX_POWER_VALUE);
    }
      
    if (newAnalogValue != m_fanAnalogValue)
    {
        // Log state changes in the fan timer for debugging
        if (m_fanAnalogValue == 0 && newAnalogValue > 0)
        {
            Serial.printf("Fan Started\n");
        }
        else if (m_fanAnalogValue > 0 && newAnalogValue == 0)
        {
            Serial.printf("Fan Stopped\n");
        } 

        m_fanAnalogValue= newAnalogValue;
        analogWrite(m_fanPin, m_fanAnalogValue);
    }    
}

void FanManager::loop(float deltaSeconds)
{
    // Update the fan activation timer based on motor state
    updateFanTimer(deltaSeconds);

    // Update the current power fraction based on the activation state
    updateFanPowerFraction();

    // Blend the motor power up or down depending on wether we want the fan active
    applyPowerFraction();
}