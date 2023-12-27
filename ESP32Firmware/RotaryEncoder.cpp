// Adapted from: https://github.com/Erriez/ErriezRotaryEncoderHalfStep

#if (defined(__AVR__) || defined(ARDUINO_ARCH_SAM) || defined(ARDUINO_ARCH_SAMD) || defined(ARDUINO_ARCH_STM32F1))
#include <avr/pgmspace.h>
#else
#include <pgmspace.h>
#endif

#include "RotaryEncoder.h"

#define DIR_NONE  0x00      //!< No complete step yet
#define DIR_CW    0x10      //!< Clockwise step
#define DIR_CCW   0x20      //!< Counter-clockwise step

// Use the half-step state table (emits a code at 00 and 11)
#define RHS_START         0x00     //!< Rotary half step start
#define RHS_CCW_BEGIN     0x01     //!< Rotary half step counter clock wise begin
#define RHS_CW_BEGIN      0x02     //!< Rotary half step clock wise begin
#define RHS_START_M       0x03     //!< Rotary half step start
#define RHS_CW_BEGIN_M    0x04     //!< Rotary half step clock wise begin
#define RHS_CCW_BEGIN_M   0x05     //!< Rotary half step counter clock wise begin

static const PROGMEM uint8_t halfStepTable[6][4] = {
    // RHS_START (00)
    {RHS_START_M,           RHS_CW_BEGIN,     RHS_CCW_BEGIN,    RHS_START},
    // RHS_CCW_BEGIN
    {RHS_START_M | DIR_CCW, RHS_START,        RHS_CCW_BEGIN,    RHS_START},
    // RHS_CW_BEGIN
    {RHS_START_M | DIR_CW,  RHS_CW_BEGIN,     RHS_START,        RHS_START},
    // RHS_START_M (11)
    {RHS_START_M,           RHS_CCW_BEGIN_M,  RHS_CW_BEGIN_M,   RHS_START},
    // RHS_CW_BEGIN_M
    {RHS_START_M,           RHS_START_M,      RHS_CW_BEGIN_M,   RHS_START | DIR_CW},
    // RHS_CCW_BEGIN_M
    {RHS_START_M,           RHS_CCW_BEGIN_M,  RHS_START_M,      RHS_START | DIR_CCW},
};

RotaryHalfStep::RotaryHalfStep(uint8_t pin1, uint8_t pin2) 
  : m_pin1(pin1)
  , m_pin2(pin2)
  , m_state(0)
  , m_position(0)
  , m_lowerBound(INT16_MIN)
  , m_upperBound(INT16_MAX) 
{
    pinMode(m_pin1, INPUT_PULLUP);
    pinMode(m_pin2, INPUT_PULLUP);
    resetEventBuffer();
}

void RotaryHalfStep::read()
{
    // Sample rotary digital pins
    int pinState = (digitalRead(m_pin1) << 1) | digitalRead(m_pin2);

    // Determine new state from the pins and state table.
    m_state = pgm_read_byte(&halfStepTable[m_state & 0x0f][pinState]);

    // Check rotary state
    int changeDirection= (m_state & 0x30);
    if (changeDirection == DIR_CW)
    {
      enqueueEvent(1);
    }
    else if (changeDirection == DIR_CCW)
    {
      enqueueEvent(-1);
    }
}

void RotaryHalfStep::setChangedHandler(CallbackFunction f)
{
  m_changeCallback = f;
}

int RotaryHalfStep::getPosition() const
{
  return m_position;
}

void RotaryHalfStep::resetPosition(int p, bool fireCallback)
{
  m_position= min(max(p, m_lowerBound), m_upperBound);  

  if (fireCallback && m_changeCallback != nullptr)
    m_changeCallback(*this);
}

void RotaryHalfStep::setIsWrapped(bool bFlag)
{
  m_bIsWrapped= bFlag;
}

void RotaryHalfStep::setUpperBound(int upper) 
{
  m_upperBound = (m_lowerBound < upper) ? upper : m_lowerBound;
}

void RotaryHalfStep::setLowerBound(int lower) 
{
  m_lowerBound = (lower < m_upperBound) ? lower : m_upperBound;
}

int RotaryHalfStep::getUpperBound() const 
{
  return m_upperBound;
}

int RotaryHalfStep::getLowerBound() const 
{
  return m_lowerBound;
}

void RotaryHalfStep::loop()
{
  int encoderEvent= 0;
  while(dequeueEvent(encoderEvent))
  {
    m_position+= encoderEvent;

    if (m_bIsWrapped)
    {
      if (m_position > m_upperBound)
        m_position= m_lowerBound;
      else if (m_position < m_lowerBound)
        m_position= m_upperBound;
    }
    else
    {
      m_position= min(max(m_position, m_lowerBound), m_upperBound);
    }

    if (m_changeCallback != nullptr)
      m_changeCallback(*this);
  }
}

void RotaryHalfStep::enqueueEvent(int event)
{
  if (m_stepCount < 1)
  {
    m_stepCount++;
    return;
  }
  m_stepCount= 0;

  m_eventCircularBuffer[m_eventWriteIndex]= event;
  m_eventWriteIndex = (m_eventWriteIndex + 1) % MAX_ROTARY_EVENTS;

  if (m_eventCount >= MAX_ROTARY_EVENTS)
  {
     Serial.println("RotaryEncoder queue overflow!");
     m_eventReadIndex = (m_eventReadIndex + 1) % MAX_ROTARY_EVENTS;
  }
  else
  {
     m_eventCount++;
  }
}

bool RotaryHalfStep::dequeueEvent(int& outEvent)
{
  if (m_eventCount > 0)
  {
    outEvent= m_eventCircularBuffer[m_eventReadIndex];
    m_eventReadIndex = (m_eventReadIndex + 1) % MAX_ROTARY_EVENTS;
    m_eventCount--;
    return true;
  }
  else
  {
    return false;
  }
}

void RotaryHalfStep::resetEventBuffer()
{
  m_eventWriteIndex= 0;
  m_eventReadIndex= 0;
  m_eventCount= 0;
  m_stepCount= 0;
}