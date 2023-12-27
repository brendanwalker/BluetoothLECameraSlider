#ifndef AppStage_MagnetTest_h
#define AppStage_MagnetTest_h

#include "App.h"
#include "AppStage.h"

class AppStage_MagnetTest : public AppStage, public InputEventListener
{
public:
	AppStage_MagnetTest(class App* app);

    static AppStage_MagnetTest* getInstance() { return s_instance; }

    virtual void enter() override;
    virtual void exit() override;  
    virtual void render() override;

    virtual void onRotaryButtonClicked(class Button2* button) override;

	static const char* APP_STAGE_NAME;	

private:
    static AppStage_MagnetTest* s_instance;
};

#endif
