#include "App.h"
#include "AppStage_MainMenu.h"
#include "AppStage_Monitor.h"
#include "AppStage_SliderSettings.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "SliderManager.h"

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

void AppStage_MainMenu::onCommand(const std::string& command)
{
  if (command == "calibrate")
  {    
    Serial.println("MainMenu: Received calibrate command");
    m_app->pushAppStage(AppStage_SliderCalibration::getInstance());
  }
  else if (command == "stop")
  {
    Serial.println("MainMenu: Received stop command");
    SliderState::getInstance()->stopAll();
  }
}

void AppStage_MainMenu::enter()
{
  Serial.println("Enter Main Menu");

  AppStage::enter();

  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(true);
  bleManager->setCommandHandler(this);

  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_MainMenu::pause()
{
  AppStage::pause();
  Serial.println("Pause Main Menu");

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(false);
  bleManager->clearCommandHandler(this);
  Serial.println("DISABLE Bluetooth command processing");
}

void AppStage_MainMenu::resume()
{
  AppStage::resume();
  Serial.println("Resume Main Menu");

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(true);
  bleManager->setCommandHandler(this);
  Serial.println("ENABLE Bluetooth command processing");
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