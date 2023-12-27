#include "Arduino.h"
#include "App.h"
#include "AppStage_SliderSettings.h"
#include "AppStage_SliderCalibration.h"
#include <Adafruit_SSD1306.h>

enum class eSliderSettingOptions : int
{
  Calibrate,
  Back,

  COUNT
};
static const String kSliderSettingsStrings[(int)eSliderSettingOptions::COUNT] = {"Calibrate", "Back"};

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
    switch((eSliderSettingOptions)optionIndex)
    {
    case eSliderSettingOptions::Calibrate:
        Serial.println("Calibration clicked");
        m_app->pushAppStage(AppStage_SliderCalibration::getInstance());
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