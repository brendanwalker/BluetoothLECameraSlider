#include "App.h"
#include "AppStage_Monitor.h"
#include "AppStage_MainMenu.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "SliderManager.h"

//-- statics ----
const char* AppStage_Monitor::APP_STAGE_NAME = "Monitor";
AppStage_Monitor* AppStage_Monitor::s_instance= nullptr;

AppStage_Monitor::AppStage_Monitor(App* app)
    : AppStage(app, AppStage_Monitor::APP_STAGE_NAME)
{
    s_instance= this; 
}

void AppStage_Monitor::enter()
{
  Serial.println("Enter Monitor Menu");

  AppStage::enter();

  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(true);
  bleManager->setCommandHandler(this);

  m_app->pushInputListener(this);
}

void AppStage_Monitor::onCommand(const std::string& command)
{
  if (command == "calibrate")
  {    
    Serial.println("MainMenu: Received calibrate command");
    AppStage_SliderCalibration* sliderCalibration= AppStage_SliderCalibration::getInstance();
    sliderCalibration->setAutoCalibration(true);
    m_app->pushAppStage(sliderCalibration);
  }
  else if (command == "stop")
  {
    Serial.println("MainMenu: Received stop command");
    SliderState::getInstance()->stopAll();
  }
  else if (command == "save")
  {
    Serial.println("MainMenu: Received save command");
    m_app->save();
  }
}

void AppStage_Monitor::pause()
{
  AppStage::pause();
  Serial.println("Pause Monitor Menu");

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(false);
  bleManager->clearCommandHandler(this);
  Serial.println("DISABLE Bluetooth command processing");  
}

void AppStage_Monitor::resume()
{
  AppStage::resume();
  Serial.println("Resume Monitor Menu");

  // Ignore commands from ArtNet when outside of the MainMenu
  BLEManager* bleManager= BLEManager::getInstance();
  bleManager->setBLEControlEnabled(true);
  bleManager->setCommandHandler(this);
  Serial.println("ENABLE Bluetooth command processing");  
}

void AppStage_Monitor::exit()
{
  Serial.println("Exit Monitor Menu");
  m_app->popInputListener();
  AppStage::exit();
}

void AppStage_Monitor::onRotaryButtonClicked(Button2* button)
{
  Serial.println("onRotaryButtonClicked");
  m_app->pushAppStage(AppStage_MainMenu::getInstance());
}

void AppStage_Monitor::render()
{
  Adafruit_SSD1306 *display = getApp()->getDisplay();

  SliderState* sliderState= SliderState::getInstance();
  float slide= sliderState->getSliderPosFraction();
  float pan= sliderState->getPanPosFraction();
  float tilt= sliderState->getTiltPosFraction();

  display->clearDisplay();
  display->setTextSize(1);
  display->setCursor(2, 2);
  display->print("Monitor");  
  display->setCursor(2, 12);
  display->printf(" Slide: %.2f", slide);
  display->setCursor(2, 22);
  display->printf("   Pan: %.2f", pan);
  display->setCursor(2, 32);
  display->printf("  Tilt: %.2f", tilt);  
  display->display();    
}