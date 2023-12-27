#ifndef AppStage_MainMenu_h
#define AppStage_MainMenu_h

#include "AppStage.h"
#include "SelectionMenu.h"

enum class eMenuMenuOptions : int
{
  INVALID= -1,

  Monitor,
  SliderSettings,
  DMXSettings,
  NetworkSettings,  
  Save,

  COUNT
};

class AppStage_MainMenu : public AppStage, public SelectionMenuListener
{
public:
	AppStage_MainMenu(class App* app);

	virtual void enter() override;
  virtual void pause() override;
  virtual void resume() override;
  virtual void exit() override;  
  virtual void render() override;

  virtual void onOptionClicked(int optionIndex) override;

	static const char* APP_STAGE_NAME;	

private:
  SelectionMenu m_selectionMenu;
};

#endif
