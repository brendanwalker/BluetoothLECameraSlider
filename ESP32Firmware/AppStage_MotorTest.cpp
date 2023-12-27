#include "App.h"
#include "AppStage_MotorTest.h"
#include "AppStage_Monitor.h"
#include "AppStage_NetworkSettings.h"
#include "AppStage_SliderSettings.h"
#include "SliderManager.h"

//-- statics ----
const char* AppStage_MotorTest::APP_STAGE_NAME = "MotorTest";
AppStage_MotorTest* AppStage_MotorTest::s_instance= nullptr;

enum class eMotorTestOptions : int
{
  INVALID= -1,

  PanMotorNeg,
  PanMotorPos,
  TiltMotorNeg,
  TiltMotorPos,
  SlideMotorNeg,
  SlideMotorPos,
  Back,

  COUNT
};

static const String kMenuStrings[(int)eMotorTestOptions::COUNT]= 
{
  "Pan Motor -45deg",
  "Pan Motor +45deg",
  "Tilt Motor -45deg",
  "Tilt Motor +45deg",
  "Slide Motor -5cm",
  "Siide Motor +5cm",    
  "Back"
};

AppStage_MotorTest::AppStage_MotorTest(App* app)
	: AppStage(app, AppStage_MotorTest::APP_STAGE_NAME)
  , m_selectionMenu("Motor Test", kMenuStrings, (int)eMotorTestOptions::COUNT)
{
    s_instance= this; 
}

void AppStage_MotorTest::enter()
{
  Serial.println("Enter Motor Test");

  AppStage::enter();
  m_selectionMenu.setListener(this);
  m_selectionMenu.show();
}

void AppStage_MotorTest::exit()
{
  m_selectionMenu.hide();
  AppStage::exit();

  Serial.println("Exit Main Menu");
}

void AppStage_MotorTest::onOptionClicked(int optionIndex)
{
  //Serial.print("Clicked ");
  switch((eMotorTestOptions)optionIndex)
  {
  case eMotorTestOptions::PanMotorNeg:
    SliderState::getInstance()->movePanStepperDegrees(-45.f);
    break;
  case eMotorTestOptions::PanMotorPos:
    SliderState::getInstance()->movePanStepperDegrees(45.f);
    break;
  case eMotorTestOptions::TiltMotorNeg:
    SliderState::getInstance()->moveTiltStepperDegrees(-45.f);
    break;
  case eMotorTestOptions::TiltMotorPos:
    SliderState::getInstance()->moveTiltStepperDegrees(45.f);
    break;
  case eMotorTestOptions::SlideMotorNeg:
    SliderState::getInstance()->moveSlideStepperMillimeters(-50.f);
    break;
  case eMotorTestOptions::SlideMotorPos:
    SliderState::getInstance()->moveSlideStepperMillimeters(50.f);
    break;
  case eMotorTestOptions::Back:
    App::getInstance()->popAppState();
    break;
  }
}

void AppStage_MotorTest::render()
{
  m_selectionMenu.render();
}