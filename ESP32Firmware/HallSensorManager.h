#ifndef HALL_SENSOR_MANAGER_H__
#define HALL_SENSOR_MANAGER_H__

#include "Arduino.h"

class HallSensorEventListener
{
public:
  virtual void onPanSensorChanged(bool bActive) {}
  virtual void onTiltSensorChanged(bool bActive) {}
  virtual void onSlideMinSensorChanged(bool bActive) {}
  virtual void onSlideMaxSensorChanged(bool bActive) {}
};

class HallSensorManager
{
public:
  HallSensorManager(uint8_t panPin, uint8_t tiltPin, uint8_t slideMinPin, uint8_t slideMaxPin);

  static HallSensorManager* getInstance() { return s_instance; }

  void setListener(HallSensorEventListener *listener);
  void clearListener(HallSensorEventListener *listener);  

  void setup();
  void loop();

  bool isPanSensorActive() const { return m_panHallState == LOW; }
  bool isTiltSensorActive() const { return m_tiltHallState == LOW; }
  bool isSlideMinSensorActive() const { return m_slideMinHallState == LOW; }
  bool isSlideMaxSensorActive() const { return m_slideMaxHallState == LOW; }

protected:
  static HallSensorManager* s_instance;

  // Listener
  HallSensorEventListener* m_listener= nullptr;  

  // Sensor Pins
  uint8_t m_panPin;
  uint8_t m_tiltPin;
  uint8_t m_slideMinPin;
  uint8_t m_slideMaxPin;

  // Sensor state
  int m_panHallState;
  int m_tiltHallState;
  int m_slideMinHallState;
  int m_slideMaxHallState;
};

#endif