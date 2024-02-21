#include "App.h"
#include "AppStage_MainMenu.h"
#include "AppStage_Test.h"
#include "AppStage_SliderSettings.h"
#include "AppStage_SliderCalibration.h"
#include "SliderManager.h"

//-- statics ----
const char* AppStage_MainMenu::APP_STAGE_NAME = "MainMenu";
AppStage_MainMenu* AppStage_MainMenu::s_instance= nullptr;

static const String kMenuStrings[(int)eMenuMenuOptions::COUNT]= 
{
  "Slider Settings",
  "Testing",
  "Save Pose",
  "Back",
};

AppStage_MainMenu::AppStage_MainMenu(App* app)
	: AppStage(app, AppStage_MainMenu::APP_STAGE_NAME)
  , m_selectionMenu("Main Menu", kMenuStrings, (int)eMenuMenuOptions::COUNT)
{
  s_instance= this; 
}

void AppStage_MainMenu::enter()
{
  Serial.println("Enter Main Menu");

  AppStage::enter();

  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_MainMenu::pause()
{
  AppStage::pause();
  Serial.println("Pause Main Menu");
}

void AppStage_MainMenu::resume()
{
  AppStage::resume();
  Serial.println("Resume Main Menu");
}

void AppStage_MainMenu::exit()
{
  m_selectionMenu.hide();
  AppStage::exit();

  Serial.println("Exit Main Menu");
}

void AppStage_MainMenu::onOptionClicked(int optionIndex)
{
  //Serial.print("Clicked ");
  switch((eMenuMenuOptions)optionIndex)
  {
  case eMenuMenuOptions::SliderSettings:
    m_app->pushAppStage(AppStage_SliderSettings::getInstance());
    break;
  case eMenuMenuOptions::Testing:
    m_app->pushAppStage(AppStage_Test::getInstance());
    break;    
  case eMenuMenuOptions::Save:
    m_app->save();
    break;
  case eMenuMenuOptions::Back:
    m_app->popAppState();
    break;
  }
}

void AppStage_MainMenu::render()
{
  m_selectionMenu.render();
}