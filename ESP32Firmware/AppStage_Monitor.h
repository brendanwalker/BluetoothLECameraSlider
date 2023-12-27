#ifndef AppStage_Monitor_h
#define AppStage_Monitor_h

#include "AppStage.h"
#include "SelectionMenu.h"

class AppStage_Monitor : public AppStage, public SelectionMenuListener
{
public:
	AppStage_Monitor(class App* app);

    static AppStage_Monitor* getInstance() { return s_instance; }

    virtual void enter() override;
    virtual void pause() override;
    virtual void resume() override;
    virtual void exit() override;  
    virtual void render() override;

    virtual void onOptionClicked(int optionIndex) override;

	static const char* APP_STAGE_NAME;	

private:
    static AppStage_Monitor* s_instance;

    SelectionMenu m_selectionMenu;
};

#endif
