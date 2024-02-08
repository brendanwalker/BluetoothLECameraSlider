#include "App.h"
#include "AppStage_Test.h"
#include "AppStage_MagnetTest.h"
#include "AppStage_MotorTest.h"

//-- statics ----
const char* AppStage_Test::APP_STAGE_NAME = "Test";
AppStage_Test* AppStage_Test::s_instance= nullptr;

enum class eMonitorOptions : int
{
  INVALID= -1,

  MagnetTest,
  MotorTest,
  Back,

  COUNT
};

static const String kTestOptions[(int)eMonitorOptions::COUNT]= 
{
  "Magnet Test",
  "Motor Test",
  "Back"
};

AppStage_Test::AppStage_Test(App* app)
    : AppStage(app, AppStage_Test::APP_STAGE_NAME)
    , m_selectionMenu("Test", kTestOptions, (int)eMonitorOptions::COUNT)
{
    s_instance= this; 
}

void AppStage_Test::enter()
{
  Serial.println("Enter Test Menu");

  AppStage::enter();
  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_Test::pause()
{
  AppStage::pause();
  Serial.println("Pause Test Menu");
}

void AppStage_Test::resume()
{
  AppStage::resume();
  Serial.println("Resume Test Menu");
}

void AppStage_Test::exit()
{
  m_selectionMenu.hide();
  AppStage::exit();

  Serial.println("Exit Test Menu");
}

void AppStage_Test::onOptionClicked(int optionIndex)
{
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

void AppStage_Test::render()
{
  m_selectionMenu.render();
}