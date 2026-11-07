#import <UIKit/UIKit.h>
#import <CoreHaptics/CoreHaptics.h>
#import <dispatch/dispatch.h>

// Reuse the engine, but give each numeric tick/award its own finite 25 ms
// event. This continues for any counting duration without a repeating player.
static bool DarkNebulaTimedImpact(float intensity, float sharpness)
{
    if (![CHHapticEngine capabilitiesForHardware].supportsHaptics) return false;
    static CHHapticEngine *engine;
    NSError *error = nil;
    if (engine == nil)
    {
        engine = [[CHHapticEngine alloc] initAndReturnError:&error];
        if (engine == nil) return false;
        engine.playsHapticsOnly = YES;
        engine.autoShutdownEnabled = YES;
    }
    if (![engine startAndReturnError:&error])
    {
        engine = nil;
        return false;
    }
    CHHapticEventParameter *strength = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticIntensity value:intensity];
    CHHapticEventParameter *texture = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticSharpness value:sharpness];
    CHHapticEvent *event = [[CHHapticEvent alloc]
        initWithEventType:CHHapticEventTypeHapticContinuous
        parameters:@[strength, texture] relativeTime:0 duration:0.025];
    CHHapticPattern *pattern = [[CHHapticPattern alloc]
        initWithEvents:@[event] parameters:@[] error:&error];
    if (pattern == nil) return false;
    id<CHHapticPatternPlayer> player = [engine createPlayerWithPattern:pattern error:&error];
    return player != nil && [player startAtTime:CHHapticTimeImmediate error:&error];
}

extern "C" void DarkNebulaScoreTick()
{
    if (DarkNebulaTimedImpact(0.75f, 0.5f)) return;
    static UIImpactFeedbackGenerator *feedback;
    if (feedback == nil)
        feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
    [feedback impactOccurredWithIntensity:1.0];
    [feedback prepare];
}

extern "C" void DarkNebulaStarAward()
{
    if (DarkNebulaTimedImpact(1.0f, 1.0f)) return;
    static UIImpactFeedbackGenerator *feedback;
    if (feedback == nil)
        feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
    [feedback impactOccurredWithIntensity:1.0];
    [feedback prepare];
}

extern "C" void DarkNebulaOpenEpisodeTwo()
{
    UIApplication *application = [UIApplication sharedApplication];
    [application openURL:[NSURL URLWithString:@"darknebula-episode2://open"]
                 options:@{}
       completionHandler:^(BOOL opened) {
        if (!opened)
            dispatch_async(dispatch_get_main_queue(), ^{
                [application openURL:[NSURL URLWithString:@"https://github.com/FluorescentLava/dark-nebula-rebuild/releases"]
                             options:@{} completionHandler:nil];
            });
    }];
}
