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

class AppStage_SliderCalibration : public AppStage, public SelectionMenuListener, public HallSensorEventListener
{
public:
    AppStage_SliderCalibration(class App *app);

    static AppStage_SliderCalibration *getInstance() { return s_instance; }

    void setAutoExitOnComplete(bool bFlag) { m_bAutoExitOnComplete= bFlag; }

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

    static const char *APP_STAGE_NAME;

private:
    void setState(eSliderCalibrationState newState);
    void onEnterState(eSliderCalibrationState newState);
    void onLeaveState(eSliderCalibrationState oldState);
    void renderProgress(const String& message);
    eSliderCalibrationState m_calibrationState = eSliderCalibrationState::INVALID;

    static AppStage_SliderCalibration *s_instance;

    bool m_bAutoExitOnComplete= false;
    SelectionMenu m_setupMenu;
    SelectionMenu m_completeMenu;
    SelectionMenu m_failedMenu;
    SelectionMenu* m_activeMenu= nullptr;
    uint8_t m_moveState= 0;
    eSearchMode m_searchMode= eSearchMode::NotStarted;
};

#endif
