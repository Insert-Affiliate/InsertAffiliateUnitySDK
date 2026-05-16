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
}
