#ifndef SliderManager_h
#define SliderManager_h

#include "Arduino.h"
#include "FastAccelStepper.h"

class SliderState
{
public:
  SliderState(
    uint8_t en_pin,
    uint8_t pan_step_pin, uint8_t pan_dir_pin,
    uint8_t tilt_step_pin, uint8_t tilt_dir_pin,
    uint8_t slide_step_pin, uint8_t slide_dir_pin
  );

  static SliderState* getInstance() { return s_instance; }

  void setup();
  void loop();
  void stopAll();  
  bool isAnySliderRunning() const;

  void setPanStepperAngularAcceleration(float cameraAngAccelDegrees);
  float getPanStepperAngularAcceleration();
  void setTiltStepperAngularAcceleration(float cameraAngAccelDegrees);
  float getTiltStepperAngularAcceleration();
  void setSlideStepperLinearAcceleration(float cameraLinAccelMM);
  float getSlideStepperLinearAcceleration();

  void setPanStepperAngularSpeed(float cameraDegreesPerSecond);
  float getPanStepperAngularSpeed();
  void setTiltStepperAngularSpeed(float cameraDegreesPerSecond);
  float getTiltStepperAngularSpeed();
  void setSlideStepperLinearSpeed(float cameraMMPerSecond);
  float getSlideStepperLinearSpeed();

  void setPanStepperTargetDegrees(float cameraDegrees);
  float getPanStepperTargetDegrees() const;
  void setTiltStepperTargetDegrees(float cameraDegrees);
  float getTiltStepperTargetDegrees() const;

  int8_t movePanStepperDegrees(float degrees);
  int8_t moveTiltStepperDegrees(float degrees);
  int8_t moveSlideStepperMillimeters(float millimeters);

  float getPanStepperDegreesRange() const;
  float getTiltStepperDegreesRange() const;
  float getSlideStepperWidthMillimeters() const;

  inline FastAccelStepper* getPanStepper() { return m_panStepper; }
  inline FastAccelStepper* getTiltStepper() { return m_tiltStepper; }
  inline FastAccelStepper* getSlideStepper() { return m_slideStepper; }

  inline int32_t getPanStepperPosition() const { return m_panStepper->getCurrentPosition(); }
  inline int32_t getTiltStepperPosition() const { return m_tiltStepper->getCurrentPosition(); }
  inline int32_t getSlideStepperPosition() const { return m_slideStepper->getCurrentPosition(); }

  void setSliderPosFraction(float fraction);
  float getSliderPosFraction(); // [-1.f, 1.f]
  void setSliderSpeedFraction(float fraction);
  float getSliderSpeedFraction(); // [0.f, 1.f]
  void setSliderAccelFraction(float fraction);
  float getSliderAccelFraction(); // [0.f, 1.f]

  void setPanPosFraction(float fraction);
  float getPanPosFraction(); // [-1.f, 1.f]
  void setPanSpeedFraction(float fraction);
  float getPanSpeedFraction(); // [0.f, 1.f]
  void setPanAccelFraction(float fraction);
  float getPanAccelFraction(); // [0.f, 1.f]

  void setTiltPosFraction(float fraction);
  float getTiltPosFraction(); // [-1.f, 1.f]
  void setTiltSpeedFraction(float fraction);
  float getTiltSpeedFraction(); // [0.f, 1.f]
  void setTiltAccelFraction(float fraction);
  float getTiltAccelFraction(); // [0.f, 1.f]

  inline bool areSteppersCalibrated() const { return m_calibrated; }
  inline void setPanStepperCenter(int32_t position) { m_panStepperCenter= position; }
  inline void setTiltStepperCenter(int32_t position) { m_tiltStepperCenter= position; }
  inline void setSlideStepperMin(int32_t position) { m_sliderStepperMin= position; }
  inline void setSlideStepperMax(int32_t position) { m_sliderStepperMax= position; }
  inline int32_t getPanStepperCenter() const { return m_panStepperCenter; }
  inline int32_t getTiltStepperCenter() const { return m_tiltStepperCenter; }
  inline int32_t getSlideStepperMin() const { return m_sliderStepperMin; }
  inline int32_t getSlideStepperMax() const { return m_sliderStepperMax; }
  void finalizeCalibration();

  void writePositionsToConfig();

private:
  static SliderState* s_instance;

  void writeCalibrationToConfig();

  float remapFloatToFloat(float inMin, float inMax, float outMin, float outMax, float inValue);
  float remapInt32ToFloat(int32_t intMin, int32_t intMax, float floatMin, float floatMax, int32_t value);
  int32_t remapFloatToInt32(float floatMin, float floatMax, int32_t intMin, int32_t intMax, float value);

  uint32_t motorAngleToSteps(float degrees) const;
  float stepsToMotorAngle(uint32_t steps) const;

  uint32_t millimetersToSteps(float millimeters) const;
  float stepsToMillimeters(uint32_t steps) const;

  uint8_t m_enPin;
  uint8_t m_panStepPin;
  uint8_t m_panDirPin;
  uint8_t m_tiltStepPin;
  uint8_t m_tiltDirPin;
  uint8_t m_slideStepPin;
  uint8_t m_slideDirPin;

  FastAccelStepperEngine m_engine;
  FastAccelStepper* m_panStepper;
  FastAccelStepper* m_tiltStepper;
  FastAccelStepper* m_slideStepper;

  int32_t m_lastTargetSlidePosition; // steps
  int32_t m_lastTargetSlideSpeed; // steps / s
  int32_t m_lastTargetSlideAcceleration; // steps / s²

  int32_t m_lastTargetPanPosition; // steps
  int32_t m_lastTargetPanSpeed; // steps / s
  int32_t m_lastTargetPanAcceleration; // steps / s²

  int32_t m_lastTargetTiltPosition; // steps
  int32_t m_lastTargetTiltSpeed; // steps / s
  int32_t m_lastTargetTiltAcceleration; // steps / s²

  int32_t m_panStepperCenter= INT_MIN;
  int32_t m_tiltStepperCenter= INT_MIN;
  int32_t m_sliderStepperMin= INT_MIN;
  int32_t m_sliderStepperMax= INT_MAX;
  bool m_calibrated= false;
};

#endif