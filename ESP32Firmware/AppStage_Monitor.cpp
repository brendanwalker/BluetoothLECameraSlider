#include "App.h"
#include "AppStage_Monitor.h"
#include "AppStage_MagnetTest.h"
#include "AppStage_MotorTest.h"

//-- statics ----
const char* AppStage_Monitor::APP_STAGE_NAME = "Monitor";
AppStage_Monitor* AppStage_Monitor::s_instance= nullptr;

enum class eMonitorOptions : int
{
  INVALID= -1,

  MagnetTest,
  MotorTest,
  Back,

  COUNT
};

static const String kMonitorOptions[(int)eMonitorOptions::COUNT]= 
{
  "Magnet Test",
  "Motor Test",
  "Back"
};

AppStage_Monitor::AppStage_Monitor(App* app)
    : AppStage(app, AppStage_Monitor::APP_STAGE_NAME)
    , m_selectionMenu("Monitor", kMonitorOptions, (int)eMonitorOptions::COUNT)
{
    s_instance= this; 
}

void AppStage_Monitor::enter()
{
  Serial.println("Enter Monitor Menu");

  AppStage::enter();
  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_Monitor::pause()
{
  AppStage::pause();
  Serial.println("Pause Monitor Menu");
}

void AppStage_Monitor::resume()
{
  AppStage::resume();
  Serial.println("Resume Monitor Menu");
}

void AppStage_Monitor::exit()
{
  m_selectionMenu.hide();
  AppStage::exit();

  Serial.println("Exit Monitor Menu");
}

void AppStage_Monitor::onOptionClicked(int optionIndex)
{
  //Serial.print("Clicked ");
  switch((eMonitorOptions)optionIndex)
  {
  case eMonitorOptions::MagnetTest:
    m_app->pushAppStage(AppStage_MagnetTest::getInstance());
    break;
  case eMonitorOptions::MotorTest:
    m_app->pushAppStage(AppStage_MotorTest::getInstance());
    break;    
  case eMonitorOptions::Back:
    m_app->popAppState();
    break;
  }
}

void AppStage_Monitor::render()
{
  m_selectionMenu.render();
}