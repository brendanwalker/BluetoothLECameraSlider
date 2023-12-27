// Adapted from: https://github.com/Erriez/ErriezRotaryEncoderHalfStep
#ifndef ROTARY_HALF_STEP_H__
#define ROTARY_HALF_STEP_H__

#include <Arduino.h>

#define MAX_ROTARY_EVENTS 16

class RotaryHalfStep
{
public:
    RotaryHalfStep(uint8_t pin1, uint8_t pin2);
    
    void read();    
    void loop();

    int getPosition() const;
    void resetPosition(int p = 0, bool fireCallback = true);    

    void setIsWrapped(bool bFlag);
    void setUpperBound(int upper_bound);
    void setLowerBound(int lower_bound);
    int getUpperBound() const;
    int getLowerBound() const;

    using CallbackFunction = void (*)(RotaryHalfStep&);
    void setChangedHandler(CallbackFunction f);

private:
    uint8_t m_pin1;
    uint8_t m_pin2;
    uint8_t m_state;
    int m_position;
    int m_lowerBound;
    int m_upperBound;
    bool m_bIsWrapped;
    
    CallbackFunction m_changeCallback = nullptr;

    int m_eventCircularBuffer[MAX_ROTARY_EVENTS];
    int m_eventWriteIndex;
    int m_eventReadIndex;
    int m_eventCount;
    int m_stepCount;

    void enqueueEvent(int event);
    bool dequeueEvent(int& outEvent);
    void resetEventBuffer();
};

#endif // ROTARY_HALF_STEP_H__