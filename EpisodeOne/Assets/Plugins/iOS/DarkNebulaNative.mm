#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>

extern "C" void DarkNebulaScoreTick()
{
    static UIImpactFeedbackGenerator *feedback;
    if (feedback == nil)
        feedback = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    [feedback impactOccurredWithIntensity:0.35];
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
