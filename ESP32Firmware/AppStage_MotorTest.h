#ifndef AppStage_MotorTest_h
#define AppStage_MotorTest_h

#include "App.h"
#include "AppStage.h"
#include "SelectionMenu.h"

class AppStage_MotorTest : public AppStage, public SelectionMenuListener
{
public:
	AppStage_MotorTest(class App* app);

    static AppStage_MotorTest* getInstance() { return s_instance; }

    virtual void enter() override;
    virtual void exit() override;  
    virtual void render() override;

    virtual void onOptionClicked(int optionIndex) override;

	static const char* APP_STAGE_NAME;	

private:
    static AppStage_MotorTest* s_instance;
    
    SelectionMenu m_selectionMenu;
};

#endif
