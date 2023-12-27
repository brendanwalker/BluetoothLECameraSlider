#include "HallSensorManager.h"

HallSensorManager* HallSensorManager::s_instance = nullptr;

HallSensorManager::HallSensorManager(uint8_t panPin, uint8_t tiltPin, uint8_t slideMinPin, uint8_t slideMaxPin)
    : m_panPin(panPin)
    , m_tiltPin(tiltPin)
    , m_slideMinPin(slideMinPin)
    , m_slideMaxPin(slideMaxPin)
    , m_panHallState(HIGH)
    , m_tiltHallState(HIGH)
    , m_slideMinHallState(HIGH)
    , m_slideMaxHallState(HIGH)
{
    s_instance= this;

    pinMode(panPin, INPUT);
    pinMode(tiltPin, INPUT);
    pinMode(slideMinPin, INPUT);
    pinMode(slideMaxPin, INPUT);
}

void HallSensorManager::setListener(HallSensorEventListener *listener)
{
    m_listener= listener;
}

void HallSensorManager::clearListener(HallSensorEventListener *listener)
{
  if (m_listener == listener)
    m_listener= nullptr;
}

void HallSensorManager::setup()
{
    m_panHallState = digitalRead(m_panPin);
    m_tiltHallState = digitalRead(m_tiltPin);
    m_slideMinHallState = digitalRead(m_slideMinPin);
    m_slideMaxHallState = digitalRead(m_slideMaxPin);
}

void HallSensorManager::loop()
{
    int prevPanState= m_panHallState;
    int prevTiltState= m_tiltHallState;
    int prevSlideMinState= m_slideMinHallState;
    int prevSlideMaxState= m_slideMaxHallState;

    m_panHallState = digitalRead(m_panPin);
    m_tiltHallState = digitalRead(m_tiltPin);
    m_slideMinHallState = digitalRead(m_slideMinPin);
    m_slideMaxHallState = digitalRead(m_slideMaxPin);

    if (m_listener != nullptr)
    {
        if (prevPanState != m_panHallState)
            m_listener->onPanSensorChanged(isPanSensorActive());
        if (prevTiltState != m_tiltHallState)
            m_listener->onTiltSensorChanged(isTiltSensorActive());
        if (prevSlideMinState != m_slideMinHallState)
            m_listener->onSlideMinSensorChanged(isSlideMinSensorActive());                   
        if (prevSlideMaxState != m_slideMaxHallState)
            m_listener->onSlideMaxSensorChanged(isSlideMaxSensorActive());            
    }
}

