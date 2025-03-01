#include "App.h"
#include "AppStage_Monitor.h"
#include "AppStage_MainMenu.h"
#include "AppStage_SliderCalibration.h"
#include "BLEManager.h"
#include "ConfigManager.h"
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

bool AppStage_Monitor::onCommand(const std::vector<std::string>& args, BLECommandResponse& results)
{
  SliderState* sliderState= SliderState::getInstance();

  if (args[0] == "reset_calibration")
  {
    Serial.println("Monitor: Received reset_calibration command");

    sliderState->resetCalibration();
    return true;
  }
  else if (args[0] == "calibrate")
  {    
    Serial.println("Monitor: Received calibrate command");
    
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
    return true;
  }
  else if (args[0] == "get_slider_calibration")
  {
    int32_t min_pos= sliderState->getSlideStepperMin();
    int32_t max_pos= sliderState->getSlideStepperMax();

    Serial.println("Monitor: Received get_slider_state command");
    
    results.addStringParam("slider_calibration");
    results.addIntParam(min_pos);
    results.addIntParam(max_pos);
    return true;
  }
  else if (args[0] == "get_motor_pan_limits")
  {
    Serial.println("Monitor: Received get_motor_pan_limits command");

    StepperMotorLimits limits;
    ConfigManager::getInstance()->getMotorLimitsConfig(limits);

    results.addStringParam("motor_pan_limits");
    results.addFloatParam(limits.panMinAngle);
    results.addFloatParam(limits.panMaxAngle);
    results.addFloatParam(limits.panMinSpeed);
    results.addFloatParam(limits.panMaxSpeed);
    results.addFloatParam(limits.panMinAcceleration);
    results.addFloatParam(limits.panMaxAcceleration);
    return true;
  }
  else if (args[0] == "get_motor_tilt_limits")
  {
    Serial.println("Monitor: Received get_motor_tilt_limits command");

    StepperMotorLimits limits;
    ConfigManager::getInstance()->getMotorLimitsConfig(limits);

    results.addStringParam("motor_tilt_limits");
    results.addFloatParam(limits.tiltMinAngle);
    results.addFloatParam(limits.tiltMaxAngle);
    results.addFloatParam(limits.tiltMinSpeed);
    results.addFloatParam(limits.tiltMaxSpeed);
    results.addFloatParam(limits.tiltMinAcceleration);
    results.addFloatParam(limits.tiltMaxAcceleration);
    return true;
  }
  else if (args[0] == "get_motor_slide_limits")
  {
    Serial.println("Monitor: Received get_slider_tilt_limits command");

    StepperMotorLimits limits;
    ConfigManager::getInstance()->getMotorLimitsConfig(limits);

    results.addStringParam("motor_slide_limits");
    results.addFloatParam(limits.slideMinSpeed);
    results.addFloatParam(limits.slideMaxSpeed);
    results.addFloatParam(limits.slideMinAcceleration);
    results.addFloatParam(limits.slideMaxAcceleration);
    return true;
  }   
  else if (args[0] == "set_pos")
  {
    float slide= (float)std::atof(args[1].c_str());
    float pan= (float)std::atof(args[2].c_str());
    float tilt= (float)std::atof(args[3].c_str());

    sliderState->setSlidePanTiltPosFraction(slide, pan, tilt);
    return true;
  }
  else if (args[0] == "move_slider" && args.size() >= 2)
  {
    int delta = std::stoi(args[1]);

    Serial.println("Monitor: Received move_slider command");
    int32_t pos= sliderState->getSlideStepperPosition();
    sliderState->setSlideStepperPosition(pos + delta);
    return true;
  }
  else if (args[0] == "set_slide_min_pos")
  {
    Serial.println("Monitor: Received set_slide_min_pos command");
    sliderState->saveSlideStepperPosAsMin();
    int32_t min_pos= sliderState->getSlideStepperMin();

    results.addStringParam("slide_min_set");
    results.addIntParam(min_pos);
    return true;
  }
  else if (args[0] == "set_slide_max_pos")
  {
    Serial.println("Monitor: Received set_slide_max_pos command");
    sliderState->saveSlideStepperPosAsMax();
    int32_t max_pos= sliderState->getSlideStepperMax();

    results.addStringParam("slide_max_set");
    results.addIntParam(max_pos);
    return true;
  }  
  else if (args[0] == "stop")
  {
    Serial.println("Monitor: Received stop command");
    sliderState->stopAll();
    return true;
  }
  else if (args[0] == "save")
  {
    Serial.println("Monitor: Received save command");
    m_app->save();
    return true;
  }

  return false;
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
  m_app->pushAppStage(AppStage_Monitor::getInstance());
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