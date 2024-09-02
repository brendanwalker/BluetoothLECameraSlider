#include "App.h"
#include "AppStage_Monitor.h"
#include "AppStage_MainMenu.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "SliderManager.h"

#include <sstream>

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

void AppStage_Monitor::onCommand(const std::vector<std::string>& args)
{
  SliderState* sliderState= SliderState::getInstance();

  if (args[0] == "reset_calibration")
  {
    Serial.println("MainMenu: Received reset_calibration command");

    sliderState->resetCalibration();
  }
  else if (args[0] == "calibrate")
  {    
    Serial.println("MainMenu: Received calibrate command");
    
    bool pan= false;
    bool tilt= false;
    bool slide= false;
    for (int arg_idx= 1; arg_idx < (int)args.size(); ++arg_idx)
    {
      const std::string& arg= args[arg_idx];

      if (arg == "pan")
      {
        pan= true;
      }
      else if (arg == "tilt")
      {
        tilt= true;
      }
      else if (arg == "slide")
      {
        slide= true;
      }       
    }

    AppStage_SliderCalibration* sliderCalibration= AppStage_SliderCalibration::getInstance();
    sliderCalibration->setDesiredCalibrations(pan, tilt, slide);
    sliderCalibration->setAutoCalibration(true);
    m_app->pushAppStage(sliderCalibration);
  }
  else if (args[0] == "get_slider_state")
  {
    int32_t pos= sliderState->getSlideStepperPosition();
    int32_t min_pos= sliderState->getSlideStepperMin();
    int32_t max_pos= sliderState->getSlideStepperMax();

    std::stringstream ss;
    ss << "slider_state " << pos << " " << min_pos << " " << max_pos;

    Serial.println("MainMenu: Received get_slider_state command");
    BLEManager::getInstance()->sendEvent(ss.str());
  }
  else if (args[0] == "move_slider" && args.size() >= 2)
  {
    int delta = std::stoi(args[1]);

    Serial.println("MainMenu: Received move_slider command");
    int32_t pos= sliderState->getSlideStepperPosition();
    sliderState->setSlideStepperPosition(pos + delta);
  }
  else if (args[0] == "set_slide_min_pos")
  {
    Serial.println("MainMenu: Received set_slide_min_pos command");
    sliderState->saveSlideStepperPosAsMin();
  }
  else if (args[0] == "set_slide_max_pos")
  {
    Serial.println("MainMenu: Received set_slide_max_pos command");
    sliderState->saveSlideStepperPosAsMax();
  }  
  else if (args[0] == "stop")
  {
    Serial.println("MainMenu: Received stop command");
    sliderState->stopAll();
  }
  else if (args[0] == "save")
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
  float slidePos= sliderState->getSliderPosFraction();
  float panPos= sliderState->getPanPosFraction();
  float tiltPos= sliderState->getTiltPosFraction();

  float speed= sliderState->getSpeedFraction();
  float accel= sliderState->getAccelFraction();

  display->clearDisplay();
  display->setTextSize(1);
  display->setCursor(2, 2);
  display->print ("    Pos   Vel   Acc");  
  display->setCursor(2, 12);
  display->printf("S: %+.2f %+.2f %+.2f", slidePos, speed, accel);
  display->setCursor(2, 22);
  display->printf("P: %+.2f %+.2f %+.2f", panPos, speed, accel);
  display->setCursor(2, 32);
  display->printf("T: %+.2f %+.2f %+.2f", tiltPos, speed, accel);  
  display->display();    
}