#ifndef App_h
#define App_h

#include <SPI.h>                            //Import libraries to control the OLED display
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

#define SCREEN_WIDTH 128                    // OLED display width, in pixels
#define SCREEN_HEIGHT 64                    // OLED display height, in pixels

#define OLED_RESET -1                       // Reset pin # (or -1 if sharing Arduino reset pin)

#define MAX_APP_STAGE_DEPTH 8
#define MAX_INPUT_LISTENER_DEPTH 8

#define AUTO_SAVE_DURATION  60              // Duration (in seconds) that we auto save the config changes

class InputEventListener
{
public:
    virtual bool getIsRotaryEncoderWrapped() const { return false; }
    virtual int getRotaryEncoderDefaultValue() const { return 0; }
    virtual int getRotaryEncoderLowerBound() const { return INT16_MIN; }
    virtual int getRotaryEncoderUpperBound() const { return INT16_MAX; }

    virtual void onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder) {}
    virtual void onRotaryButtonClicked(class Button2* button) {}
};

class App
{
public:
    App(
      class Adafruit_SSD1306* display, 
      class RotaryHalfStep* rotaryEncoder);

    static App* getInstance() { return s_instance; }

    void setup();
    void loop(float deltaSeconds);
    void save();

    inline int getAppStageStackSize() const
    {
        return m_appStageStackIndex + 1;
    }

    inline class AppStage *getCurrentAppStage() const
    {
        return (getAppStageStackSize() > 0) ? m_appStageStack[m_appStageStackIndex] : nullptr;
    }

    inline class AppStage *getParentAppStage() const
    {
        return (getAppStageStackSize() > 1) ? m_appStageStack[m_appStageStackIndex - 1] : nullptr;
    }

    inline class Adafruit_SSD1306* getDisplay() const { return m_display; } 
    inline class RotaryHalfStep* getRotaryEncoder() const { return m_rotaryEncoder; }

    void pushAppStage(AppStage *appStage);
    void popAppState();

    // Input Events
    void onRotaryEncoderValueChanged(class RotaryHalfStep* rotaryEncoder);
    void onRotaryButtonClicked(class Button2* button);
    void pushInputListener(InputEventListener *inputListener);
    void popInputListener();

private:
    void applyInputListenerSettings();

    static App* s_instance;

    // Time
    float autoSaveTimer= AUTO_SAVE_DURATION;

    // App Stages
    int m_appStageStackIndex = -1;
    class AppStage *m_appStageStack[MAX_APP_STAGE_DEPTH];

    // Input Listener Stack
    int m_inputStackIndex = -1;
    InputEventListener *m_inputListenerStack[MAX_INPUT_LISTENER_DEPTH];

    // Display
    class Adafruit_SSD1306 *m_display;

    // Rotary Encoder
    class RotaryHalfStep* m_rotaryEncoder;
};

#endif