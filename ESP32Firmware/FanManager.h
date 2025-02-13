#ifndef FAN_MANAGER_H__
#define FAN_MANAGER_H__

#include "Arduino.h"

class FanManager
{
public:
  FanManager(uint8_t fanPin);

  static FanManager* getInstance() { return s_instance; }

  void setup();
  void loop(float deltaSeconds);

  void engageFan();
  bool isFanActive() const { return m_fanPowerFraction > 0.f; }

protected:
  void updateFanTimer(float deltaSeconds);
  void updateFanPowerFraction();
  void applyPowerFraction();

  static FanManager* s_instance;

  // Fan Pin
  uint8_t m_fanPin;

  // Fan state
  float m_fanActiveTimer;
  float m_fanPowerFraction;
  int m_fanAnalogValue;
};

#endif