﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using UIKit;
#if __IOS__
using QuickLook;
#endif

namespace Xamarin.Essentials
{
    public static partial class Launcher
    {
        static Task<bool> PlatformCanOpenAsync(Uri uri) =>
            Task.FromResult(UIApplication.SharedApplication.CanOpenUrl(WebUtils.GetNativeUrl(uri)));

        static Task PlatformOpenAsync(Uri uri) =>
            PlatformOpenAsync(WebUtils.GetNativeUrl(uri));

        internal static Task<bool> PlatformOpenAsync(NSUrl nativeUrl) =>
            Platform.HasOSVersion(10, 0)
                ? UIApplication.SharedApplication.OpenUrlAsync(nativeUrl, new UIApplicationOpenUrlOptions())
                : Task.FromResult(UIApplication.SharedApplication.OpenUrl(nativeUrl));

        static Task<bool> PlatformTryOpenAsync(Uri uri)
        {
            var nativeUrl = WebUtils.GetNativeUrl(uri);

            if (UIApplication.SharedApplication.CanOpenUrl(nativeUrl))
                return PlatformOpenAsync(nativeUrl);

            return Task.FromResult(false);
        }

#if __IOS__
        static UIDocumentInteractionController documentController;

        static Task PlatformOpenAsync(OpenFileRequest request)
        {
            documentController = new UIDocumentInteractionController()
            {
                Name = request.File.FileName,
                Url = NSUrl.FromFilename(request.File.FullPath),
                Uti = request.File.ContentType
            };

            var view = Platform.GetCurrentUIViewController().View;

            CGRect rect;

            if (request.PresentationSourceBounds != Rectangle.Empty)
            {
                rect = request.PresentationSourceBounds.ToPlatformRectangle();
            }
            else
            {
                rect = DeviceInfo.Idiom == DeviceIdiom.Tablet
                    ? new CGRect(new CGPoint(view.Bounds.Width / 2, view.Bounds.Height), CGRect.Empty.Size)
                    : view.Bounds;
            }

            documentController.PresentOpenInMenu(rect, view, true);
            return Task.CompletedTask;
        }

        static Task PlatformOpenAsync(OpenFileRequest request, bool openInApp)
        {
            if (openInApp)
            {
                UIApplication.SharedApplication?.KeyWindow?.RootViewController?.
                   PresentViewController(
                       new QLPreviewController() { DataSource = new PreviewDataSource(request.File.FullPath, request.File.FileName) },
                       true,
                       null);
            }
            else
            {
                PlatformOpenAsync(request);
            }
            return Task.CompletedTask;
        }
#else
        static Task PlatformOpenAsync(OpenFileRequest request) =>
            throw new FeatureNotSupportedException();

        static Task PlatformOpenAsync(OpenFileRequest request, bool openInApp) =>
            throw new FeatureNotSupportedException();
#endif
    }

#if __IOS__
    public class PreviewDataSource : QLPreviewControllerDataSource
    {
        readonly string filePath;
        readonly string fileName;

        public PreviewDataSource(string filePath, string fileName)
        {
            this.filePath = filePath;
            this.fileName = fileName;
        }

        public override IQLPreviewItem GetPreviewItem(QLPreviewController controller, nint index)
            => new PreviewItem(filePath, fileName);

        public override nint PreviewItemCount(QLPreviewController controller) => 1;
    }

    public class PreviewItem : QLPreviewItem
    {
        public PreviewItem(string filePath, string fileName)
        {
            ItemUrl = NSUrl.FromFilename(filePath);
            ItemTitle = fileName;
        }

        public override string ItemTitle { get; }

        public override NSUrl ItemUrl { get; }
    }
#endif
}
