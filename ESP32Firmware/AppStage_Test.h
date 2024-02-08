#ifndef AppStage_Test_h
#define AppStage_Test_h

#include "AppStage.h"
#include "SelectionMenu.h"

class AppStage_Test : public AppStage, public SelectionMenuListener
{
public:
	AppStage_Test(class App* app);

    static AppStage_Test* getInstance() { return s_instance; }

    virtual void enter() override;
    virtual void pause() override;
    virtual void resume() override;
    virtual void exit() override;  
    virtual void render() override;

    virtual void onOptionClicked(int optionIndex) override;

	static const char* APP_STAGE_NAME;	

private:
    static AppStage_Test* s_instance;

    SelectionMenu m_selectionMenu;
};

#endif
