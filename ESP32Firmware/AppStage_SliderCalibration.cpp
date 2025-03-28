#include "AppStage_SliderCalibration.h"

#include "Arduino.h"
#include "App.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "SliderManager.h"

#include <Adafruit_SSD1306.h>

enum class eSliderReadyOptions : int
{
    Start,
    Cancel,

    COUNT
};
static const String kSliderReadyStrings[(int)eSliderReadyOptions::COUNT] = {"Start", "Cancel"};

enum class eCalibrationDoneOptions : int
{
    Ok,
    Redo,

    COUNT
};
static const String kCalibrationDoneStrings[(int)eCalibrationDoneOptions::COUNT] = {"Ok", "Redo"};

const char *AppStage_SliderCalibration::APP_STAGE_NAME = "SliderCalibration";
AppStage_SliderCalibration *AppStage_SliderCalibration::s_instance = nullptr;

AppStage_SliderCalibration::AppStage_SliderCalibration(App *app)
    : AppStage(app, AppStage_SliderCalibration::APP_STAGE_NAME)
    , m_setupMenu("Slider Calibration", kSliderReadyStrings, (int)eSliderReadyOptions::COUNT)
    , m_completeMenu("Calibration Complete", kCalibrationDoneStrings, (int)eCalibrationDoneOptions::COUNT)
    , m_failedMenu("Failed Calibration", kCalibrationDoneStrings, (int)eCalibrationDoneOptions::COUNT)
{
    s_instance = this;
}

void AppStage_SliderCalibration::enter()
{
    Serial.println("Enter Slider Calibration");
    AppStage::enter();

    BLEManager::getInstance()->setCommandHandler(this);
    HallSensorManager::getInstance()->setListener(this);
    setState(eSliderCalibrationState::Setup);
}

void AppStage_SliderCalibration::exit()
{
    Serial.println("Exit Slider Calibration");
    AppStage::exit();

    setState(eSliderCalibrationState::INVALID);
    HallSensorManager::getInstance()->clearListener(this);
    BLEManager::getInstance()->clearCommandHandler(this);
}

// BLEManager Events
bool AppStage_SliderCalibration::onCommand(const std::vector<std::string>& args, BLECommandResponse& results)
{
    if (args[0] == "stop")
    {
        Serial.println("AppStage_SliderCalibration: Received stop command. Halt calibration.");
        SliderState::getInstance()->stopAll();
        m_app->popAppState();
        return true;
    }
    else
    {
        Serial.printf("AppStage_SliderCalibration: Ignoring command %s", args[0].c_str());
        return false;
    }
}

// HallSensorEventListener Events
void AppStage_SliderCalibration::onSlideMinSensorChanged(bool bActive)
{
    if (m_calibrationState == eSliderCalibrationState::FindSlideMin && bActive)
    {
        SliderState* silderState= SliderState::getInstance();

        // Store the slider min position
        int32_t sliderPosition= silderState->getSlideStepperPosition();
        silderState->setSlideStepperMin(sliderPosition);
        Serial.printf("Found slider min: %d\n", sliderPosition);

        // Stop!
        silderState->getSlideStepper()->forceStopAndNewPosition(sliderPosition);

        setState(eSliderCalibrationState::FindSlideMax);
    }
}

void AppStage_SliderCalibration::onSlideMaxSensorChanged(bool bActive)
{
    if (m_calibrationState == eSliderCalibrationState::FindSlideMax && bActive)
    {
        SliderState* silderState= SliderState::getInstance();

        // Store the slider max position
        int32_t sliderPosition= silderState->getSlideStepperPosition();
        silderState->setSlideStepperMax(sliderPosition);
        Serial.printf("Found slider max: %d\n", sliderPosition);

        // Stop!
        silderState->getSlideStepper()->forceStopAndNewPosition(sliderPosition);

        // Send the slider back to the midpoint while we start centering pan and yaw
        int32_t midpointPosition= (silderState->getSlideStepperMax() + silderState->getSlideStepperMin()) / 2;
        silderState->getSlideStepper()->moveTo(midpointPosition);
        Serial.printf("Return to mid point: %d\n", midpointPosition);

        setState(dequeueNextCalibrationState());       
    }
}

void AppStage_SliderCalibration::onPanSensorChanged(bool bActive)
{
    if (m_calibrationState == eSliderCalibrationState::FindPanCenter && bActive)
    {
        SliderState* silderState= SliderState::getInstance();

        // Store the pan center position
        int32_t panPosition= silderState->getPanStepperPosition();
        silderState->setPanStepperCenter(panPosition);
        Serial.printf("Found pan center: %d\n", panPosition);

        // Stop!
        silderState->getPanStepper()->stopMove();

        setState(dequeueNextCalibrationState());           
    }
}

void AppStage_SliderCalibration::onTiltSensorChanged(bool bActive)
{
    if (m_calibrationState == eSliderCalibrationState::FindTiltCenter && bActive)
    {
        SliderState* silderState= SliderState::getInstance();

        // Store the tilt center position
        int32_t tiltPosition= silderState->getTiltStepperPosition();
        silderState->setTiltStepperCenter(tiltPosition);
        Serial.printf("Found tilt center: %d\n", tiltPosition);

        // Stop!
        silderState->getTiltStepper()->stopMove();

        setState(dequeueNextCalibrationState());            
    }
}

// Button Bypass for Hall Effect Sensor searches
void AppStage_SliderCalibration::onRotaryButtonClicked(Button2* button)
{
    Serial.println("onRotaryButtonClicked");

    if (m_calibrationState == eSliderCalibrationState::FindSlideMin)
    {
        Serial.println("Bypass find slide min");
        onSlideMinSensorChanged(true);
    }
    else if (m_calibrationState == eSliderCalibrationState::FindSlideMax)
    {
        Serial.println("Bypass find slide max");
        onSlideMaxSensorChanged(true);
    }
    else if (m_calibrationState == eSliderCalibrationState::FindPanCenter)
    {
        Serial.println("Bypass find pan center");
        onPanSensorChanged(true);
    }
    else if (m_calibrationState == eSliderCalibrationState::FindTiltCenter)
    {
        Serial.println("Bypass find tilt center");
        onTiltSensorChanged(true);
    }
}

// Selection Events
void AppStage_SliderCalibration::onOptionClicked(int optionIndex)
{
    Serial.println("onOptionClicked");

    if (m_calibrationState == eSliderCalibrationState::Setup)
    {
        switch ((eSliderReadyOptions)optionIndex)
        {
        case eSliderReadyOptions::Start:
            {
                setState(dequeueNextCalibrationState());
            }
            break;
        case eSliderReadyOptions::Cancel:
            m_app->popAppState();
            break;
        }
    }
    else if (m_calibrationState == eSliderCalibrationState::Complete || 
             m_calibrationState == eSliderCalibrationState::Failed)
    {
        switch ((eCalibrationDoneOptions)optionIndex)
        {
        case eCalibrationDoneOptions::Ok:
            m_app->popAppState();
            break;
        case eCalibrationDoneOptions::Redo:
            setState(eSliderCalibrationState::Setup);
            break;
        }        
    }
}

eSliderCalibrationState AppStage_SliderCalibration::dequeueNextCalibrationState()
{
    if (m_bWantsSlideCalibration)
    {
        m_bWantsSlideCalibration= false;
        return eSliderCalibrationState::FindSlideMin;
    }
    else if (m_bWantsPanCalibration)
    {
        m_bWantsPanCalibration= false;
        return eSliderCalibrationState::FindPanCenter;
    }
    else if (m_bWantsTiltCalibration)
    {
        m_bWantsTiltCalibration= false;
        return eSliderCalibrationState::FindTiltCenter;
    }
    else
    {
        return eSliderCalibrationState::Recenter;
    }
}

void AppStage_SliderCalibration::update(float deltaSeconds)
{
    SliderState* sliderState= SliderState::getInstance();
    eSliderCalibrationState nextState= m_calibrationState;

    switch (m_calibrationState)
    {
    case eSliderCalibrationState::Setup:
        if (m_bAutoCalibrate)
        {
            Serial.println("Auto-starting slider calibration.");
            nextState= dequeueNextCalibrationState();
        }
        break;
    case eSliderCalibrationState::FindSlideMin:
    case eSliderCalibrationState::FindSlideMax:
        if (!sliderState->getSlideStepper()->isRunning())
        {
            Serial.println("Failed to find slider end!");
            nextState= eSliderCalibrationState::Failed;
        }
        break;
    case eSliderCalibrationState::FindPanCenter:
        {
            if (!sliderState->getPanStepper()->isRunning())
            {
                if (m_searchMode == eSearchMode::Idle)
                {
                    Serial.printf("Start Pan Center positive search -> %.1f\n", m_searchScanSize);
                    m_searchMode = eSearchMode::SearchPositive;
                    sliderState->movePanStepperDegrees(m_searchScanSize);
                }
                else if (m_searchMode == eSearchMode::SearchPositive)
                {
                    m_searchScanSize= min(m_searchScanSize*2.f, m_searchScanMaxSize);
                    Serial.printf("Start Pan Center negative search -> -%.1f\n", m_searchScanSize);
                    m_searchMode = eSearchMode::SearchNegative;
                    sliderState->movePanStepperDegrees(-m_searchScanSize);
                }
                else
                {
                    if (m_searchScanSize < m_searchScanMaxSize)
                    {
                        m_searchScanSize= min(m_searchScanSize*2.f, m_searchScanMaxSize);
                        m_searchMode = eSearchMode::Idle;
                    }
                    else
                    {
                        Serial.println("Failed to find Pan Center!");
                        nextState= eSliderCalibrationState::Failed;                    
                    }
                }
            }
        }
        break;
    case eSliderCalibrationState::FindTiltCenter:
        {
            if (!sliderState->getTiltStepper()->isRunning())
            {
                if (m_searchMode == eSearchMode::Idle)
                {
                    Serial.printf("Start Tilt Center positive search -> %.1f\n", m_searchScanSize);
                    m_searchMode = eSearchMode::SearchPositive;
                    sliderState->moveTiltStepperDegrees(m_searchScanSize);
                }
                else if (m_searchMode == eSearchMode::SearchPositive)
                {
                    m_searchScanSize= min(m_searchScanSize*2.f, m_searchScanMaxSize);
                    Serial.printf("Start Tilt Center negative search -> -%.1f\n", m_searchScanSize);
                    m_searchMode = eSearchMode::SearchNegative;
                    sliderState->moveTiltStepperDegrees(-m_searchScanSize);
                }
                else
                {
                    if (m_searchScanSize < m_searchScanMaxSize)
                    {
                        m_searchScanSize= min(m_searchScanSize*2.f, m_searchScanMaxSize);
                        m_searchMode = eSearchMode::Idle;
                    }
                    else
                    {
                        Serial.println("Failed to find Tilt Center!");
                        nextState= eSliderCalibrationState::Failed;                    
                    }
                }
            }            
        }
        break;
    case eSliderCalibrationState::Recenter:
        {
            // Wait for the slider to finish to recenter
            if (!sliderState->isAnySliderRunning())
            {
                sliderState->finalizeCalibration();
                nextState= eSliderCalibrationState::Complete;
            }
        }
        break;
    case eSliderCalibrationState::Complete:
        if (m_bAutoCalibrate)
        {
            m_bAutoCalibrate= false;
            m_app->popAppState();
        }
        break;
    }

    if (m_calibrationState != nextState)
    {
        setState(nextState);
    }
}

void AppStage_SliderCalibration::render()
{
    switch (m_calibrationState)
    {
    case eSliderCalibrationState::Setup:
    case eSliderCalibrationState::Complete:
    case eSliderCalibrationState::Failed:
        m_activeMenu->render();
        break;
    case eSliderCalibrationState::FindSlideMin:
        {
            renderProgress("Find Slider Min...");
        }
        break;
    case eSliderCalibrationState::FindSlideMax:
        {
            renderProgress("Find Slider Max...");
        }
        break;
    case eSliderCalibrationState::FindPanCenter:
        {
            renderProgress("Find Pan Center...");
        }
        break;
    case eSliderCalibrationState::FindTiltCenter:
        {
            renderProgress("Find Tilt Center...");
        }
        break;
    case eSliderCalibrationState::Recenter:
        {
            renderProgress("Recentering...");
        }
        break;        
    }
}

void AppStage_SliderCalibration::renderProgress(const String& message)
{
    Adafruit_SSD1306 *display = getApp()->getDisplay();

    SliderState* sliderState= SliderState::getInstance();
    int32_t pan= sliderState->getPanStepper()->getCurrentPosition();
    int32_t panTarget= sliderState->getPanStepper()->targetPos();

    int32_t tilt= sliderState->getTiltStepper()->getCurrentPosition();
    int32_t tiltTarget= sliderState->getTiltStepper()->targetPos();

    int32_t slide= sliderState->getSlideStepper()->getCurrentPosition();
    int32_t slideTarget= sliderState->getSlideStepper()->targetPos();

    display->clearDisplay();
    display->setTextSize(1);
    display->setCursor(2, 2);
    display->print(message);
    display->setCursor(2, 12);
    display->printf("P: %d->%d", pan, panTarget);
    display->setCursor(2, 22);
    display->printf("T: %d->%d", tilt, tiltTarget);
    display->setCursor(2, 32);
    display->printf("S: %d->%d", slide, slideTarget);
    display->display();    
}

void AppStage_SliderCalibration::setState(eSliderCalibrationState newState)
{
    if (newState != m_calibrationState)
    {
        onLeaveState(m_calibrationState);
        m_calibrationState = newState;
        onEnterState(m_calibrationState);
    }
}

void AppStage_SliderCalibration::onEnterState(eSliderCalibrationState newState)
{
    BLEManager* bleManager= BLEManager::getInstance();
    SliderState* sliderState= SliderState::getInstance();
    ConfigManager* configManager= ConfigManager::getInstance();

    m_activeMenu= nullptr;

    switch (m_calibrationState)
    {
    case eSliderCalibrationState::Setup:
        {
            bleManager->setStatus("calibration_started");

            m_activeMenu = &m_setupMenu;

            // Stop any slider motion
            sliderState->stopAll();

            // Set slider speed for calibration
            sliderState->setPanStepperAngularSpeed(PAN_CALIBRATION_SPEED);
            sliderState->setTiltStepperAngularSpeed(TILT_CALIBRATION_SPEED);
            sliderState->setSlideStepperLinearSpeed(SLIDE_CALIBRATION_SPEED);        

            // Reset slider positions to defaults
            sliderState->getPanStepper()->setCurrentPosition(DEFAULT_INITIAL_PAN_POSITION);
            sliderState->getTiltStepper()->setCurrentPosition(DEFAULT_INITIAL_TILT_POSITION);
            sliderState->getSlideStepper()->setCurrentPosition(DEFAULT_INITIAL_SLIDE_POSITION);
        }
        break;
    case eSliderCalibrationState::FindSlideMin:
        {
            // Run backward until we hit the slider min hall effect sensor
            m_moveState= sliderState->getSlideStepper()->moveTo(DEFAULT_INITIAL_SLIDE_POSITION + MAX_SLIDER_SEARCH_STEPS);
        }
        break;
    case eSliderCalibrationState::FindSlideMax:
        // Run forward until we hit the slider min hall effect sensor
        m_moveState= sliderState->getSlideStepper()->moveTo(DEFAULT_INITIAL_SLIDE_POSITION - MAX_SLIDER_SEARCH_STEPS);
        break;
    case eSliderCalibrationState::FindPanCenter:
        {
            // Let update() manage the search state
            m_searchMode= eSearchMode::Idle;
            m_searchScanMaxSize= 180.f;
            m_searchScanSize= m_searchScanMaxSize / 8;
        }
        break;
    case eSliderCalibrationState::FindTiltCenter:
        {
            // Let update() manage the search state
            m_searchMode= eSearchMode::Idle;
            m_searchScanMaxSize= 60.f;
            m_searchScanSize= m_searchScanMaxSize / 4;
        }    
        break;
    case eSliderCalibrationState::Complete:
        bleManager->setStatus("calibration_completed");
        m_activeMenu = &m_completeMenu;
        break;
    case eSliderCalibrationState::Failed:
        bleManager->setStatus("calibration_failed");
        m_activeMenu = &m_failedMenu;
        break;
    default:
        break;
    }

    if (m_activeMenu != nullptr)
    {
        m_activeMenu->setListener(this);
        m_activeMenu->show();
    }
    else if (m_calibrationState != eSliderCalibrationState::INVALID)
    {
        App::getInstance()->pushInputListener(this);
    }
}

void AppStage_SliderCalibration::onLeaveState(eSliderCalibrationState oldState)
{
    if (m_activeMenu != nullptr)
    {
        m_activeMenu->hide();
        m_activeMenu->clearListener();
        m_activeMenu = nullptr;
    }
    else if (m_calibrationState != eSliderCalibrationState::INVALID)
    {
        App::getInstance()->popInputListener();
    }
}