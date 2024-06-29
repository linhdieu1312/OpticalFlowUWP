#include "pch.h"
#include "OpenCVHelper.h"
#include <opencv2/video/tracking.hpp>
#include <opencv2\core\core.hpp>
#include <opencv2\imgproc\imgproc.hpp>
using namespace std;
using namespace OpenCVComp;
using namespace Platform;
using namespace Windows::Graphics::Imaging;
using namespace Windows::Foundation;
using namespace Microsoft::WRL;
using namespace cv;


struct __declspec(uuid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")) __declspec(novtable) IMemoryBufferByteAccess : ::IUnknown
{
	virtual HRESULT __stdcall GetBuffer(uint8_t** value, uint32_t* capacity) = 0;
};

OpenCVHelper::OpenCVHelper() {};

bool OpenCVHelper::GetPointerToPixelData(SoftwareBitmap^ bitmap, unsigned char** pPixelData, unsigned int* capacity)
{
	BitmapBuffer^ bmpBuffer = bitmap->LockBuffer(BitmapBufferAccessMode::ReadWrite);
	IMemoryBufferReference^ reference = bmpBuffer->CreateReference();

	ComPtr<IMemoryBufferByteAccess> pBufferByteAccess;
	if ((reinterpret_cast<IInspectable*>(reference)->QueryInterface(IID_PPV_ARGS(&pBufferByteAccess))) != S_OK)
	{
		return false;
	}

	if (pBufferByteAccess->GetBuffer(pPixelData, capacity) != S_OK)
	{
		return false;
	}
	return true;
}

bool OpenCVHelper::TryConvert(SoftwareBitmap^ from, Mat& convertedMat)
{
	unsigned char* pPixels = nullptr;
	unsigned int capacity = 0;
	if (!GetPointerToPixelData(from, &pPixels, &capacity))
	{
		return false;
	}

	Mat mat(from->PixelHeight,
		from->PixelWidth,
		CV_8UC4, // assume input SoftwareBitmap is BGRA8
		(void*)pPixels);

	// shallow copy because we want convertedMat.data = pPixels
	// don't use .copyTo or .clone
	convertedMat = mat;
	return true;
}

void OpenCVHelper::Track(SoftwareBitmap^ prevBM, SoftwareBitmap^ curBM, const Platform::Array<Windows::Foundation::Point>^ prevPoints, Platform::WriteOnlyArray<Windows::Foundation::Point>^ curPoints, Platform::WriteOnlyArray<BYTE>^ pointStatus, int maxIterations, double epsilon, int wSize)
{
	TermCriteria termcrit(TermCriteria::COUNT | TermCriteria::EPS, maxIterations, epsilon);
	Mat gray, prevGray, image, prevImage;
	TryConvert(curBM, image);
	cvtColor(image, gray, COLOR_BGR2GRAY);
	TryConvert(prevBM, prevImage);
	cvtColor(prevImage, prevGray, COLOR_BGR2GRAY);
	vector<Point2f> points[2];
	cv::Size subPixWinSize(10, 10), winSize(wSize, wSize);

	for (int i = 0; i < prevPoints->Length; i++)
	{
		Point2f point;
		point.x = prevPoints[i].X;
		point.y = prevPoints[i].Y;
		vector<Point2f> tmp;
		tmp.push_back(point);
		cornerSubPix(gray, tmp, winSize, cv::Size(-1, -1), termcrit);
		points[0].push_back(tmp[0]);
	}

	vector<uchar> status;
	vector<float> err;
	calcOpticalFlowPyrLK(prevGray, gray, points[0], points[1], status, err, winSize, 3, termcrit, 0, 0.001);

	for (int i = 0; i < points[1].size(); i++)
	{
		curPoints->Data[i].X = points[1][i].x;
		curPoints->Data[i].Y = points[1][i].y;
		pointStatus->Data[i] = status[i];
	}

	gray.release();
	prevGray.release();
	image.release();
	prevImage.release();
	points[0].clear();
	points[1].clear();
	points->clear();
}