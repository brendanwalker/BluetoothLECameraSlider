#include "Arduino.h"
#include "App.h"
#include "AppStage_SliderSettings.h"
#include "AppStage_SliderCalibration.h"
#include <Adafruit_SSD1306.h>

enum class eSliderSettingOptions : int
{
  CalibratePan,
  CalibrateTilt,
  CalibrateSlide,
  Back,

  COUNT
};
static const String kSliderSettingsStrings[(int)eSliderSettingOptions::COUNT] = {"Find Pan Center", "Find Tilt Center", "Find Slide Limits", "Back"};

const char* AppStage_SliderSettings::APP_STAGE_NAME = "SliderSettings";
AppStage_SliderSettings* AppStage_SliderSettings::s_instance= nullptr;

AppStage_SliderSettings::AppStage_SliderSettings(App* app)
	: AppStage(app, AppStage_SliderSettings::APP_STAGE_NAME)
  , m_selectionMenu("Slider Settings", kSliderSettingsStrings, (int)eSliderSettingOptions::COUNT)
{ 
  s_instance= this;
}

void AppStage_SliderSettings::enter()
{
  Serial.println("Enter Slider Settings");

  AppStage::enter();
  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_SliderSettings::exit()
{
  m_selectionMenu.hide();
  AppStage::exit();

  Serial.println("Exit Slider Settings");
}

// Selection Events
void AppStage_SliderSettings::onOptionClicked(int optionIndex)
{
    AppStage_SliderCalibration* calibrator= AppStage_SliderCalibration::getInstance();

    switch((eSliderSettingOptions)optionIndex)
    {
    case eSliderSettingOptions::CalibratePan:
        Serial.println("Pan Calibration clicked");
        calibrator->setDesiredCalibrations(true, false, false);
        m_app->pushAppStage(calibrator);
        break;
    case eSliderSettingOptions::CalibrateTilt:
        Serial.println("Tilt Calibration clicked");
        calibrator->setDesiredCalibrations(false, true, false);
        m_app->pushAppStage(calibrator);
        break;
    case eSliderSettingOptions::CalibrateSlide:
        Serial.println("Slide Calibration clicked");
        calibrator->setDesiredCalibrations(false, false, true);
        m_app->pushAppStage(calibrator);
        break;                
    case eSliderSettingOptions::Back:
        Serial.println("Back clicked");
        m_app->popAppState();
        break;        
    }
}

void AppStage_SliderSettings::render()
{
  m_selectionMenu.render();
}