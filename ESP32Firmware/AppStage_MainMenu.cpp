#include "App.h"
#include "AppStage_MainMenu.h"
#include "AppStage_Monitor.h"
#include "AppStage_SliderSettings.h"
#include "BLEManager.h"

//-- statics ----
const char* AppStage_MainMenu::APP_STAGE_NAME = "MainMenu";

static const String kMenuStrings[(int)eMenuMenuOptions::COUNT]= 
{
  "Monitor",
  "Slider Settings",
  "Save"
};

AppStage_MainMenu::AppStage_MainMenu(App* app)
	: AppStage(app, AppStage_MainMenu::APP_STAGE_NAME)
  , m_selectionMenu("Main Menu", kMenuStrings, (int)eMenuMenuOptions::COUNT)
{ 
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

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager::getInstance()->setBLECommandsEnabled(false);
  Serial.println("DISABLE ArtNet command processing");
}

void AppStage_MainMenu::resume()
{
  AppStage::resume();
  Serial.println("Resume Main Menu");

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager::getInstance()->setBLECommandsEnabled(true);
  Serial.println("ENABLE ArtNet command processing");
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
  case eMenuMenuOptions::Monitor:
    m_app->pushAppStage(AppStage_Monitor::getInstance());
    break;
  case eMenuMenuOptions::SliderSettings:
    m_app->pushAppStage(AppStage_SliderSettings::getInstance());
    break;
  case eMenuMenuOptions::Save:
    m_app->save();
    break;
  }
}

void AppStage_MainMenu::render()
{
  m_selectionMenu.render();
}