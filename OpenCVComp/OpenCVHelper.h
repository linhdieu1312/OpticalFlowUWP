#pragma once

#include <opencv2\core\core.hpp>
#include <opencv2\imgproc\imgproc.hpp>

namespace OpenCVComp
{
	public ref class OpenCVHelper sealed
	{
	public:
		OpenCVHelper();
		void Track(Windows::Graphics::Imaging::SoftwareBitmap^ prevBM, Windows::Graphics::Imaging::SoftwareBitmap^ curBM, const Platform::Array<Windows::Foundation::Point>^ prevPoints, Platform::WriteOnlyArray<Windows::Foundation::Point>^ curPoints, Platform::WriteOnlyArray<BYTE>^ pointStatus, int maxIterations, double epsilon, int wSize);
	private:
		bool TryConvert(Windows::Graphics::Imaging::SoftwareBitmap^ from, cv::Mat& convertedMat);
		bool GetPointerToPixelData(Windows::Graphics::Imaging::SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity);
	};
}
