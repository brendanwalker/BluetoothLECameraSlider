#ifndef AppStage_MainMenu_h
#define AppStage_MainMenu_h

#include "AppStage.h"
#include "SelectionMenu.h"
#include "BLEManager.h"

enum class eMenuMenuOptions : int
{
  INVALID= -1,

  Monitor,
  SliderSettings,
  Save,

  COUNT
};

class AppStage_MainMenu : 
  public AppStage, 
  public SelectionMenuListener,
  public BLECommandHandler
{
public:
	AppStage_MainMenu(class App* app);

	virtual void enter() override;
  virtual void pause() override;
  virtual void resume() override;
  virtual void exit() override;  
  virtual void render() override;

  // SelectionMenuListener
  virtual void onOptionClicked(int optionIndex) override;

  // BLECommandHandler
  virtual void onCommand(const std::string& command) override;
  
	static const char* APP_STAGE_NAME;	

private:
  SelectionMenu m_selectionMenu;
};

#endif
