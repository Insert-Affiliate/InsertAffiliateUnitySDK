#import <UIKit/UIKit.h>

extern "C" {
    const char* _InsertAffiliate_GetIOSVersion() {
        NSString* version = [[UIDevice currentDevice] systemVersion];
        const char* str = [version UTF8String];
        char* result = (char*)malloc(strlen(str) + 1);
        strcpy(result, str);
        return result;
    }

    const char* _InsertAffiliate_GetClipboardString() {
        NSString* content = [[UIPasteboard generalPasteboard] string];
        if (content == nil) return NULL;
        const char* str = [content UTF8String];
        char* result = (char*)malloc(strlen(str) + 1);
        strcpy(result, str);
        return result;
    }

    // Opens the system share sheet with the referral text (in-app referrals).
    void _InsertAffiliate_ShareText(const char* text) {
        if (text == NULL) return;
        NSString* message = [NSString stringWithUTF8String:text];

        dispatch_async(dispatch_get_main_queue(), ^{
            UIWindow* window = nil;
            for (UIWindow* candidate in [UIApplication sharedApplication].windows) {
                if (candidate.isKeyWindow) { window = candidate; break; }
            }
            if (window == nil) window = [UIApplication sharedApplication].windows.firstObject;

            UIViewController* presenter = window.rootViewController;
            while (presenter.presentedViewController != nil) {
                presenter = presenter.presentedViewController;
            }
            if (presenter == nil) return;

            UIActivityViewController* sheet =
                [[UIActivityViewController alloc] initWithActivityItems:@[message] applicationActivities:nil];
#if !__has_feature(objc_arc)
            [sheet autorelease];
#endif

            // iPad presents the sheet as a popover, which needs an anchor.
            if (sheet.popoverPresentationController != nil) {
                sheet.popoverPresentationController.sourceView = presenter.view;
                sheet.popoverPresentationController.sourceRect =
                    CGRectMake(CGRectGetMidX(presenter.view.bounds), CGRectGetMidY(presenter.view.bounds), 0, 0);
                sheet.popoverPresentationController.permittedArrowDirections = 0;
            }

            [presenter presentViewController:sheet animated:YES completion:nil];
        });
    }
}
