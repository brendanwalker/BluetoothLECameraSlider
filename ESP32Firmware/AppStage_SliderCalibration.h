#ifndef AppStage_SliderCalibration_h
#define AppStage_SliderCalibration_h

#include "AppStage.h"
#include "ConfigManager.h"
#include "HallSensorManager.h"
#include "SelectionMenu.h"

enum class eSliderCalibrationState : int
{
    INVALID = -1,

    Setup,
    FindSlideMin,
    FindSlideMax,
    FindPanCenter,
    FindTiltCenter,
    Recenter,
    Complete,
    Failed,

    COUNT
};

enum class eSearchMode : int
{
    NotStarted,
    SearchCounterClockwise,
    SearchClockwise,

    COUNT
};

class AppStage_SliderCalibration : 
  public AppStage, 
  public InputEventListener,
  public SelectionMenuListener, 
  public HallSensorEventListener
{
public:
    AppStage_SliderCalibration(class App *app);

    static AppStage_SliderCalibration *getInstance() { return s_instance; }

    void setAutoCalibration(bool bFlag) { m_bAutoCalibrate= bFlag; }

    virtual void enter() override;
    virtual void exit() override;
    virtual void update(float deltaSeconds) override;
    virtual void render() override;    

    // HallSensorEventListener Events
    virtual void onPanSensorChanged(bool bActive) override;
    virtual void onTiltSensorChanged(bool bActive) override;
    virtual void onSlideMinSensorChanged(bool bActive) override;
    virtual void onSlideMaxSensorChanged(bool bActive) override;

    // Selection Events
    virtual void onOptionClicked(int optionIndex) override;

    // Input Event Handling (active when menu is not active)
    virtual bool getIsRotaryEncoderWrapped() const override { return true; }
    virtual int getRotaryEncoderDefaultValue() const override { return 0; }
    virtual int getRotaryEncoderLowerBound() const override { return 0; }
    virtual int getRotaryEncoderUpperBound() const override { return 100; }  
    virtual void onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder) override {}
    virtual void onRotaryButtonClicked(class Button2* button) override;    

    static const char *APP_STAGE_NAME;

private:
    void setState(eSliderCalibrationState newState);
    void onEnterState(eSliderCalibrationState newState);
    void onLeaveState(eSliderCalibrationState oldState);
    void renderProgress(const String& message);
    eSliderCalibrationState m_calibrationState = eSliderCalibrationState::INVALID;

    static AppStage_SliderCalibration *s_instance;

    bool m_bAutoCalibrate= false;
    SelectionMenu m_setupMenu;
    SelectionMenu m_completeMenu;
    SelectionMenu m_failedMenu;
    SelectionMenu* m_activeMenu= nullptr;
    uint8_t m_moveState= 0;
    eSearchMode m_searchMode= eSearchMode::NotStarted;
    bool m_bBypassHallSensorEvent= false;
};

#endif
