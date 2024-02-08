#ifndef AppStage_Monitor_h
#define AppStage_Monitor_h

#include "AppStage.h"
#include "BLEManager.h"

class AppStage_Monitor : 
    public AppStage, 
    public BLECommandHandler,
    public InputEventListener
{
public:
	AppStage_Monitor(class App* app);

    static AppStage_Monitor* getInstance() { return s_instance; }

    virtual void enter() override;
    virtual void pause() override;
    virtual void resume() override;
    virtual void exit() override;  
    virtual void render() override;

    // BLECommandHandler
    virtual void onCommand(const std::string& command) override;

    // Input Event Handling (active when menu is not active)
    virtual bool getIsRotaryEncoderWrapped() const override { return true; }
    virtual int getRotaryEncoderDefaultValue() const override { return 0; }
    virtual int getRotaryEncoderLowerBound() const override { return 0; }
    virtual int getRotaryEncoderUpperBound() const override { return 100; }  
    virtual void onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder) override {}
    virtual void onRotaryButtonClicked(class Button2* button) override;        

	static const char* APP_STAGE_NAME;	

private:
    static AppStage_Monitor* s_instance;
};

#endif
