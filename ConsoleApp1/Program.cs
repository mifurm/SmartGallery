

using System;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            VisionServiceClient vision = new VisionServiceClient("42198bba606b4cbe93beb6ea9801be6f",
                @"https://westeurope.api.cognitive.microsoft.com/vision/v1.0");
            VisualFeature[] features = new VisualFeature[] { VisualFeature.Description };
            AnalysisResult result = null;
            try
            {
                result = vision.AnalyzeImageAsync(@"https://wawcodestorage.blob.core.windows.net/photos/test3.jpg", 
                    new VisualFeature[] { }).Result;
            }
            catch (Exception e)
            {

            }
        }
    }
}
