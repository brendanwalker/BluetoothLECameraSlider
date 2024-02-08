#ifndef AppStage_MainMenu_h
#define AppStage_MainMenu_h

#include "AppStage.h"
#include "SelectionMenu.h"

enum class eMenuMenuOptions : int
{
  INVALID= -1,

  SliderSettings,
  Save,
  Back,

  COUNT
};

class AppStage_MainMenu : 
  public AppStage, 
  public SelectionMenuListener
{
public:
	AppStage_MainMenu(class App* app);

  static AppStage_MainMenu* getInstance() { return s_instance; }

	virtual void enter() override;
  virtual void pause() override;
  virtual void resume() override;
  virtual void exit() override;  
  virtual void render() override;

  // SelectionMenuListener
  virtual void onOptionClicked(int optionIndex) override;
  
	static const char* APP_STAGE_NAME;	

private:
  static AppStage_MainMenu* s_instance;

  SelectionMenu m_selectionMenu;
};

#endif
