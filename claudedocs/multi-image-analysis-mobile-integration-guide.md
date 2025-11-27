# Multi-Image Plant Analysis - Mobile Integration Guide (Flutter)

## Overview

This guide helps the Flutter mobile team integrate the new **Multi-Image Plant Analysis** feature. It provides a side-by-side comparison with the existing single-image implementation and outlines required changes.

**Target Audience:** Flutter Mobile Development Team
**API Version:** 1.0
**Last Updated:** 2025-02-27

---

## ⚠️ CRITICAL: Image Format Requirements

**The API expects images as BASE64 DATA URI strings, NOT URLs!**

### Request Format (CORRECT)
```json
{
  "image": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEA...",
  "leafTopImage": "data:image/jpeg;base64,/9j/4AAQSkZJRg...",
  ...
}
```

### What Happens Behind the Scenes
1. **Client (Mobile)** → Sends base64 data URI strings in request body
2. **API** → Receives base64, optimizes each image to ~100KB
3. **API** → Uploads optimized images to storage service
4. **API** → Receives URLs from storage
5. **API** → Stores URLs in database
6. **API** → Sends URLs (not base64) to RabbitMQ for N8N processing
7. **Response** → Client receives analysisId and image URLs

**Key Takeaway:** Your mobile app sends base64 data URIs, API handles everything else!

---

## Table of Contents

1. [Feature Summary](#feature-summary)
2. [API Comparison: Single vs Multi-Image](#api-comparison-single-vs-multi-image)
3. [Required Changes](#required-changes)
4. [Data Models](#data-models)
5. [API Service Layer](#api-service-layer)
6. [UI/UX Recommendations](#uiux-recommendations)
7. [Image Handling](#image-handling)
8. [Error Handling](#error-handling)
9. [Testing Checklist](#testing-checklist)

---

## Feature Summary

### What's New?
- Upload **1-5 images** per analysis instead of just 1
- **Specialized image types**: Main, Leaf Top, Leaf Bottom, Plant Overview, Root
- **Image format**: Send as base64 data URI strings (same as single-image)
- **Enhanced accuracy** through multi-perspective AI analysis
- **Same quota cost**: 1 multi-image analysis = 1 request (same as single-image)
- **Separate queue**: Uses different RabbitMQ queue to prevent workflow conflicts

### When to Use?
- **Single-Image:** Quick checks, simple issues, limited image availability
- **Multi-Image:** Comprehensive diagnosis, complex issues, maximum accuracy

---

## API Comparison: Single vs Multi-Image

### Endpoint URLs

| Feature | Single-Image | Multi-Image |
|---------|--------------|-------------|
| **Base Path** | `/api/v1/plantanalysis` | `/api/v1/plantanalysis` |
| **Submit Analysis** | `POST /async` | `POST /async/multi-image` |
| **Get Results** | `GET /{analysisId}` | `GET /{analysisId}` *(same)* |
| **Check Health** | `GET /async/health` | `GET /async/multi-image/health` |

### Request Payload Comparison

#### Single-Image Request
```json
{
  "image": "data:image/jpeg;base64,...",  // ONLY 1 IMAGE
  "cropType": "Tomato",
  "location": "Greenhouse A1",
  "gpsCoordinates": { "lat": 39.9334, "lng": 32.8597 },
  "urgencyLevel": "Medium",
  "notes": "Yellowing leaves",
  // ... other metadata
}
```

#### Multi-Image Request
```json
{
  // ========== UP TO 5 IMAGES ==========
  "image": "data:image/jpeg;base64,...",              // REQUIRED
  "leafTopImage": "data:image/jpeg;base64,...",       // OPTIONAL
  "leafBottomImage": "data:image/jpeg;base64,...",    // OPTIONAL
  "plantOverviewImage": "data:image/jpeg;base64,...", // OPTIONAL
  "rootImage": "data:image/jpeg;base64,...",          // OPTIONAL

  // ========== SAME METADATA AS SINGLE-IMAGE ==========
  "cropType": "Tomato",
  "location": "Greenhouse A1",
  "gpsCoordinates": { "lat": 39.9334, "lng": 32.8597 },
  "urgencyLevel": "Medium",
  "notes": "Comprehensive analysis needed",
  // ... all other fields identical to single-image
}
```

### Response Comparison

#### Single-Image Response (Submit)
```json
{
  "success": true,
  "message": "Analysis queued successfully",
  "data": {
    "analysisId": "async_analysis_20250227_143022_a1b2c3d4",
    "queueStatus": "Queued",
    "estimatedProcessingTime": "20-40 seconds"
  }
}
```

#### Multi-Image Response (Submit)
```json
{
  "success": true,
  "message": "Multi-image analysis queued successfully",
  "data": {
    "analysisId": "async_multi_analysis_20250227_143022_a1b2c3d4",
    "queueStatus": "Queued",
    "estimatedProcessingTime": "30-60 seconds",
    "imagesProcessed": 5,                           // NEW FIELD
    "imageUrls": {                                   // NEW FIELD
      "main": "https://freeimage.host/i/abc123",
      "leafTop": "https://freeimage.host/i/abc124",
      "leafBottom": "https://freeimage.host/i/abc125",
      "plantOverview": "https://freeimage.host/i/abc126",
      "root": "https://freeimage.host/i/abc127"
    }
  }
}
```

#### Results Response (GET /{analysisId})

**Single-Image Results:**
```json
{
  "success": true,
  "data": {
    "analysisId": "async_analysis_...",
    "analysisStatus": "Completed",
    "imageUrl": "https://freeimage.host/i/abc123",  // ONLY 1 URL
    // ... all analysis data
  }
}
```

**Multi-Image Results:**
```json
{
  "success": true,
  "data": {
    "analysisId": "async_multi_analysis_...",
    "analysisStatus": "Completed",

    // ========== MULTIPLE IMAGE URLS ==========
    "imageUrl": "https://freeimage.host/i/abc123",       // Main image
    "leafTopUrl": "https://freeimage.host/i/abc124",     // NEW
    "leafBottomUrl": "https://freeimage.host/i/abc125",  // NEW
    "plantOverviewUrl": "https://freeimage.host/i/abc126", // NEW
    "rootUrl": "https://freeimage.host/i/abc127",        // NEW

    // ========== ALL OTHER FIELDS IDENTICAL ==========
    "species": "Solanum lycopersicum",
    "overallHealthScore": 72,
    "pestsDetected": [...],
    "diseasesDetected": [...],
    "immediateActions": [...],
    // ... same structure as single-image
  }
}
```

**Key Differences:**
- Multi-image includes 5 URL fields instead of 1
- All other response fields are **identical**
- Analysis structure (health, pests, recommendations) unchanged

---

## Required Changes

### 1. Data Models

#### Existing Model (Single-Image)
```dart
class PlantAnalysisRequest {
  final String image;              // Base64 data URI
  final String cropType;
  final String location;
  final GpsCoordinates? gpsCoordinates;
  final String? urgencyLevel;
  final String? notes;
  // ... other fields
}
```

#### New Model (Multi-Image)
```dart
class MultiImagePlantAnalysisRequest {
  // ========== IMAGES (Base64 Data URI Format) ==========
  final String image;                    // REQUIRED: Main image (data:image/jpeg;base64,...)
  final String? leafTopImage;            // OPTIONAL: Leaf top view (data:image/jpeg;base64,...)
  final String? leafBottomImage;         // OPTIONAL: Leaf bottom view (data:image/jpeg;base64,...)
  final String? plantOverviewImage;      // OPTIONAL: Plant overview (data:image/jpeg;base64,...)
  final String? rootImage;               // OPTIONAL: Root system (data:image/jpeg;base64,...)

  // ========== METADATA (Same as single-image) ==========
  final String cropType;
  final String location;
  final GpsCoordinates? gpsCoordinates;
  final String? urgencyLevel;
  final String? notes;
  final String? fieldId;
  final int? altitude;
  final double? temperature;
  final double? humidity;
  final String? weatherConditions;
  final String? soilType;
  final DateTime? plantingDate;
  final DateTime? expectedHarvestDate;
  final DateTime? lastFertilization;
  final DateTime? lastIrrigation;
  final List<String>? previousTreatments;
  final ContactInfo? contactInfo;
  final Map<String, dynamic>? additionalInfo;
  // ... sponsorship fields if needed

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> json = {
      'image': image,
      'cropType': cropType,
      'location': location,
    };

    // Add optional images only if provided
    if (leafTopImage != null) json['leafTopImage'] = leafTopImage;
    if (leafBottomImage != null) json['leafBottomImage'] = leafBottomImage;
    if (plantOverviewImage != null) json['plantOverviewImage'] = plantOverviewImage;
    if (rootImage != null) json['rootImage'] = rootImage;

    // Add other optional fields
    if (gpsCoordinates != null) json['gpsCoordinates'] = gpsCoordinates!.toJson();
    if (urgencyLevel != null) json['urgencyLevel'] = urgencyLevel;
    if (notes != null) json['notes'] = notes;
    // ... add all other optional fields

    return json;
  }
}
```

#### Response Models

**Submit Response:**
```dart
class MultiImageAnalysisSubmitResponse {
  final String analysisId;
  final String queueStatus;
  final String estimatedProcessingTime;
  final int? imagesProcessed;           // NEW: Number of images uploaded
  final ImageUrls? imageUrls;           // NEW: All uploaded image URLs

  factory MultiImageAnalysisSubmitResponse.fromJson(Map<String, dynamic> json) {
    return MultiImageAnalysisSubmitResponse(
      analysisId: json['analysisId'] as String,
      queueStatus: json['queueStatus'] as String,
      estimatedProcessingTime: json['estimatedProcessingTime'] as String,
      imagesProcessed: json['imagesProcessed'] as int?,
      imageUrls: json['imageUrls'] != null
          ? ImageUrls.fromJson(json['imageUrls'] as Map<String, dynamic>)
          : null,
    );
  }
}

class ImageUrls {
  final String? main;
  final String? leafTop;
  final String? leafBottom;
  final String? plantOverview;
  final String? root;

  ImageUrls({
    this.main,
    this.leafTop,
    this.leafBottom,
    this.plantOverview,
    this.root,
  });

  factory ImageUrls.fromJson(Map<String, dynamic> json) {
    return ImageUrls(
      main: json['main'] as String?,
      leafTop: json['leafTop'] as String?,
      leafBottom: json['leafBottom'] as String?,
      plantOverview: json['plantOverview'] as String?,
      root: json['root'] as String?,
    );
  }
}
```

**Analysis Results:**
```dart
class PlantAnalysisResult {
  final int id;
  final String analysisId;
  final String analysisStatus;
  final DateTime? analysisDate;

  // ========== IMAGE URLS ==========
  final String? imageUrl;              // Main image (always present)
  final String? leafTopUrl;            // NEW: Leaf top image (multi-image only)
  final String? leafBottomUrl;         // NEW: Leaf bottom image (multi-image only)
  final String? plantOverviewUrl;      // NEW: Plant overview (multi-image only)
  final String? rootUrl;               // NEW: Root image (multi-image only)

  // ========== ALL OTHER FIELDS (Same for both) ==========
  final String? species;
  final String? variety;
  final int? overallHealthScore;
  final List<PestDetected>? pestsDetected;
  final List<DiseaseDetected>? diseasesDetected;
  final List<Recommendation>? immediateActions;
  // ... all existing fields

  factory PlantAnalysisResult.fromJson(Map<String, dynamic> json) {
    return PlantAnalysisResult(
      id: json['id'] as int,
      analysisId: json['analysisId'] as String,
      analysisStatus: json['analysisStatus'] as String,
      analysisDate: json['analysisDate'] != null
          ? DateTime.parse(json['analysisDate'] as String)
          : null,

      // Image URLs
      imageUrl: json['imageUrl'] as String?,
      leafTopUrl: json['leafTopUrl'] as String?,           // NEW
      leafBottomUrl: json['leafBottomUrl'] as String?,     // NEW
      plantOverviewUrl: json['plantOverviewUrl'] as String?, // NEW
      rootUrl: json['rootUrl'] as String?,                 // NEW

      // All other existing fields...
      species: json['species'] as String?,
      variety: json['variety'] as String?,
      overallHealthScore: json['overallHealthScore'] as int?,
      // ... parse all other fields
    );
  }

  // Helper method to get all available image URLs
  List<String> getAllImageUrls() {
    final List<String> urls = [];
    if (imageUrl != null) urls.add(imageUrl!);
    if (leafTopUrl != null) urls.add(leafTopUrl!);
    if (leafBottomUrl != null) urls.add(leafBottomUrl!);
    if (plantOverviewUrl != null) urls.add(plantOverviewUrl!);
    if (rootUrl != null) urls.add(rootUrl!);
    return urls;
  }

  // Helper method to check if this is a multi-image analysis
  bool isMultiImageAnalysis() {
    return leafTopUrl != null ||
           leafBottomUrl != null ||
           plantOverviewUrl != null ||
           rootUrl != null;
  }
}
```

---

### 2. API Service Layer

#### Existing Service (Single-Image)
```dart
class PlantAnalysisApiService {
  final Dio _dio;

  Future<ApiResponse<AnalysisSubmitResponse>> submitAnalysis(
    PlantAnalysisRequest request,
  ) async {
    try {
      final response = await _dio.post(
        '/api/v1/plantanalysis/async',
        data: request.toJson(),
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => AnalysisSubmitResponse.fromJson(json),
      );
    } catch (e) {
      // Error handling
    }
  }

  Future<ApiResponse<PlantAnalysisResult>> getAnalysisResult(
    String analysisId,
  ) async {
    try {
      final response = await _dio.get(
        '/api/v1/plantanalysis/$analysisId',
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => PlantAnalysisResult.fromJson(json),
      );
    } catch (e) {
      // Error handling
    }
  }
}
```

#### New Service Methods (Multi-Image)
```dart
class PlantAnalysisApiService {
  final Dio _dio;

  // ========== EXISTING SINGLE-IMAGE METHOD (Keep unchanged) ==========
  Future<ApiResponse<AnalysisSubmitResponse>> submitAnalysis(
    PlantAnalysisRequest request,
  ) async {
    // ... existing implementation
  }

  // ========== NEW MULTI-IMAGE METHOD ==========
  Future<ApiResponse<MultiImageAnalysisSubmitResponse>> submitMultiImageAnalysis(
    MultiImagePlantAnalysisRequest request,
  ) async {
    try {
      final response = await _dio.post(
        '/api/v1/plantanalysis/async/multi-image',  // Different endpoint
        data: request.toJson(),
        options: Options(
          sendTimeout: const Duration(seconds: 60),  // Longer timeout for multiple images
          receiveTimeout: const Duration(seconds: 30),
        ),
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => MultiImageAnalysisSubmitResponse.fromJson(json),
      );
    } on DioException catch (e) {
      if (e.response?.statusCode == 403) {
        throw QuotaExceededException(e.response?.data['message']);
      } else if (e.response?.statusCode == 503) {
        throw QueueUnavailableException('Analysis service temporarily unavailable');
      }
      rethrow;
    } catch (e) {
      throw ApiException('Failed to submit multi-image analysis: $e');
    }
  }

  // ========== SHARED METHOD (Works for both single and multi-image) ==========
  Future<ApiResponse<PlantAnalysisResult>> getAnalysisResult(
    String analysisId,
  ) async {
    // Same implementation - works for both types
    // Response automatically includes multi-image URLs if available
    try {
      final response = await _dio.get(
        '/api/v1/plantanalysis/$analysisId',
      );

      return ApiResponse.fromJson(
        response.data,
        (json) => PlantAnalysisResult.fromJson(json),
      );
    } catch (e) {
      // Error handling
    }
  }

  // ========== NEW HEALTH CHECK METHOD ==========
  Future<bool> checkMultiImageQueueHealth() async {
    try {
      final response = await _dio.get(
        '/api/v1/plantanalysis/async/multi-image/health',
      );

      final data = response.data['data'] as Map<String, dynamic>;
      return data['isHealthy'] as bool? ?? false;
    } catch (e) {
      return false;
    }
  }
}
```

---

### 3. State Management (Provider/Bloc/Riverpod)

**Example using Provider:**

```dart
class PlantAnalysisProvider extends ChangeNotifier {
  final PlantAnalysisApiService _apiService;

  AnalysisSubmitResponse? _currentAnalysis;
  PlantAnalysisResult? _analysisResult;
  bool _isLoading = false;
  String? _error;

  // ========== EXISTING: Single-Image Submit ==========
  Future<void> submitSingleImageAnalysis(PlantAnalysisRequest request) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final response = await _apiService.submitAnalysis(request);
      if (response.success) {
        _currentAnalysis = response.data;
        // Start polling for results
        _pollForResults(response.data!.analysisId);
      } else {
        _error = response.message;
      }
    } catch (e) {
      _error = 'Failed to submit analysis: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // ========== NEW: Multi-Image Submit ==========
  Future<void> submitMultiImageAnalysis(
    MultiImagePlantAnalysisRequest request,
  ) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      // Optional: Check queue health first
      final isHealthy = await _apiService.checkMultiImageQueueHealth();
      if (!isHealthy) {
        _error = 'Analysis service is currently unavailable. Please try again later.';
        _isLoading = false;
        notifyListeners();
        return;
      }

      final response = await _apiService.submitMultiImageAnalysis(request);
      if (response.success) {
        _currentAnalysis = AnalysisSubmitResponse(
          analysisId: response.data!.analysisId,
          queueStatus: response.data!.queueStatus,
          estimatedProcessingTime: response.data!.estimatedProcessingTime,
        );
        // Start polling for results (same polling logic)
        _pollForResults(response.data!.analysisId);
      } else {
        _error = response.message;
      }
    } on QuotaExceededException catch (e) {
      _error = e.message;
      // Show subscription upgrade prompt
    } on QueueUnavailableException catch (e) {
      _error = e.message;
    } catch (e) {
      _error = 'Failed to submit multi-image analysis: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  // ========== SHARED: Polling Logic (Works for both) ==========
  Future<void> _pollForResults(String analysisId) async {
    const maxAttempts = 20;
    const pollInterval = Duration(seconds: 5);
    int attempts = 0;

    while (attempts < maxAttempts) {
      await Future.delayed(pollInterval);

      try {
        final response = await _apiService.getAnalysisResult(analysisId);
        if (response.success && response.data != null) {
          final result = response.data!;

          if (result.analysisStatus == 'Completed') {
            _analysisResult = result;
            notifyListeners();
            return;
          } else if (result.analysisStatus == 'Failed') {
            _error = 'Analysis failed. Please try again.';
            notifyListeners();
            return;
          }
          // Still processing, continue polling
        }
      } catch (e) {
        // Log error but continue polling
        debugPrint('Polling error: $e');
      }

      attempts++;
    }

    // Timeout after max attempts
    _error = 'Analysis is taking longer than expected. Please check back later.';
    notifyListeners();
  }

  // Getters and other methods...
}
```

---

## UI/UX Recommendations

### 1. Image Selection Screen

**Single-Image (Existing):**
```
┌─────────────────────────────┐
│  Take Photo / Choose Image  │
│  [Single Image Selector]    │
└─────────────────────────────┘
```

**Multi-Image (New):**
```
┌─────────────────────────────────────────────────┐
│  Analysis Type:                                 │
│  ○ Quick Check (1 image)                        │
│  ● Comprehensive (up to 5 images)               │
│                                                 │
│  ┌────────────┐  ┌────────────┐                │
│  │   Main     │  │ Leaf Top   │                │
│  │  Required  │  │  Optional  │                │
│  │   [✓]      │  │   [+]      │                │
│  └────────────┘  └────────────┘                │
│                                                 │
│  ┌────────────┐  ┌────────────┐  ┌──────────┐ │
│  │Leaf Bottom │  │  Overview  │  │   Root   │ │
│  │  Optional  │  │  Optional  │  │ Optional │ │
│  │   [+]      │  │   [+]      │  │   [+]    │ │
│  └────────────┘  └────────────┘  └──────────┘ │
│                                                 │
│  Tips: Add multiple angles for better accuracy │
│                                                 │
│  [ Continue ]                                   │
└─────────────────────────────────────────────────┘
```

### 2. Image Preview & Labeling

**Show clear labels for each image type:**
- Main Image: "Primary concern"
- Leaf Top: "Top view of leaf"
- Leaf Bottom: "Bottom view (for pests)"
- Overview: "Full plant view"
- Root: "Root system"

**Allow users to:**
- Reorder images
- Remove and re-add images
- View tips for each image type

### 3. Upload Progress

**Multi-Image Specific:**
```
Uploading Images...
Main image: ✓ Uploaded
Leaf top: ⏳ Uploading... 45%
Leaf bottom: ⏳ In queue
Plant overview: ⏳ In queue
Root image: ⏳ In queue

Processing 2 of 5 images...
```

### 4. Results Display

**Image Gallery:**
```
┌────────────────────────────────────┐
│  Analysis Results                  │
│                                    │
│  Images (5):                       │
│  [Main] [Leaf Top] [Leaf Bot] ... │
│  ← Swipe to view all →             │
│                                    │
│  Health Score: 72/100              │
│  Status: Moderate Issues           │
│  ...                               │
└────────────────────────────────────┘
```

**Comparison View:**
- Allow users to toggle between images
- Highlight which image detected specific issues
- Show confidence scores per image

---

## Image Handling

### 1. Image Capture & Conversion to Base64 Data URI

**Important:** The API expects images in base64 data URI format (e.g., `data:image/jpeg;base64,/9j/4AAQ...`), NOT URLs.

**Implementation:**
```dart
import 'dart:convert';
import 'dart:io';
import 'package:image_picker/image_picker.dart';

class ImageCaptureHelper {
  final ImagePicker _picker = ImagePicker();

  /// Capture image from camera or gallery
  Future<File?> captureImage({
    required ImageSource source,
    int maxWidth = 1920,
    int maxHeight = 1080,
    int imageQuality = 85,
  }) async {
    try {
      final XFile? photo = await _picker.pickImage(
        source: source,
        maxWidth: maxWidth.toDouble(),
        maxHeight: maxHeight.toDouble(),
        imageQuality: imageQuality,
      );

      if (photo != null) {
        return File(photo.path);
      }
      return null;
    } catch (e) {
      debugPrint('Error capturing image: $e');
      return null;
    }
  }

  /// Convert image file to base64 data URI format
  /// This is what the API expects in the request body
  Future<String> imageToDataUri(File imageFile) async {
    final bytes = await imageFile.readAsBytes();
    final base64String = base64Encode(bytes);

    // Determine MIME type from file extension
    final extension = imageFile.path.split('.').last.toLowerCase();
    final mimeType = _getMimeType(extension);

    // Return data URI format: "data:image/jpeg;base64,/9j/4AAQ..."
    return 'data:$mimeType;base64,$base64String';
  }

  String _getMimeType(String extension) {
    switch (extension) {
      case 'jpg':
      case 'jpeg':
        return 'image/jpeg';
      case 'png':
        return 'image/png';
      case 'webp':
        return 'image/webp';
      default:
        return 'image/jpeg';
    }
  }
}
```

**Usage Example:**
```dart
// Capture image
File? imageFile = await ImageCaptureHelper().captureImage(
  source: ImageSource.camera,
);

if (imageFile != null) {
  // Convert to base64 data URI
  String dataUri = await ImageCaptureHelper().imageToDataUri(imageFile);

  // dataUri now contains: "data:image/jpeg;base64,/9j/4AAQ..."
  // This is what you send to the API
  print('Data URI ready for API: ${dataUri.substring(0, 50)}...');
}
```

### 2. Image Optimization (Optional but Recommended)

**Client-Side Optimization Before Converting to Base64:**

The API will optimize images server-side to ~100KB, but optimizing on client can reduce upload time.

```dart
import 'package:image/image.dart' as img;
import 'package:path_provider/path_provider.dart';

class ImageOptimizer {
  /// Optimize image before converting to base64 data URI
  /// This reduces upload size and time
  static Future<File> optimizeImage(
    File imageFile, {
    int maxWidth = 1920,
    int maxHeight = 1080,
    int quality = 85,
  }) async {
    // Read image
    final bytes = await imageFile.readAsBytes();
    final image = img.decodeImage(bytes);

    if (image == null) {
      throw Exception('Failed to decode image');
    }

    // Resize if needed
    img.Image resized = image;
    if (image.width > maxWidth || image.height > maxHeight) {
      resized = img.copyResize(
        image,
        width: image.width > maxWidth ? maxWidth : null,
        height: image.height > maxHeight ? maxHeight : null,
      );
    }

    // Compress to JPEG
    final compressed = img.encodeJpg(resized, quality: quality);

    // Save to temporary file
    final tempDir = await getTemporaryDirectory();
    final tempFile = File('${tempDir.path}/optimized_${DateTime.now().millisecondsSinceEpoch}.jpg');
    await tempFile.writeAsBytes(compressed);

    return tempFile;
  }
}
```

**Complete Flow: Capture → Optimize → Convert to Base64 Data URI:**
```dart
// Step 1: Capture image
File? originalImage = await ImageCaptureHelper().captureImage(
  source: ImageSource.camera,
);

if (originalImage != null) {
  // Step 2: Optimize (optional but recommended)
  File optimizedImage = await ImageOptimizer.optimizeImage(originalImage);

  // Step 3: Convert to base64 data URI
  String dataUri = await ImageCaptureHelper().imageToDataUri(optimizedImage);

  // Step 4: Use in API request
  final request = MultiImagePlantAnalysisRequest(
    image: dataUri,  // Base64 data URI format
    // ... other fields
  );
}
```

### 3. Memory Management for Multi-Image Uploads

**Important:** Managing 5 base64-encoded images requires careful memory handling.

```dart
class MultiImageUploadManager {
  final List<File> _tempFiles = [];

  /// Process image: Capture → Optimize → Convert to Base64 Data URI
  Future<String?> processAndStoreImage(File imageFile) async {
    try {
      // Step 1: Optimize image (reduces memory usage)
      final optimized = await ImageOptimizer.optimizeImage(imageFile);
      _tempFiles.add(optimized);

      // Step 2: Convert to base64 data URI
      final dataUri = await ImageCaptureHelper().imageToDataUri(optimized);

      // dataUri is now: "data:image/jpeg;base64,/9j/4AAQ..."
      return dataUri;
    } catch (e) {
      debugPrint('Error processing image: $e');
      return null;
    }
  }

  /// Clean up temporary files after upload completes
  void cleanup() {
    // Delete temporary files to free memory
    for (final file in _tempFiles) {
      try {
        if (file.existsSync()) {
          file.deleteSync();
        }
      } catch (e) {
        debugPrint('Error deleting temp file: $e');
      }
    }
    _tempFiles.clear();
  }
}

// Usage in widget:
@override
void dispose() {
  // Always cleanup temp files when widget is disposed
  _uploadManager.cleanup();
  super.dispose();
}
```

**Complete Multi-Image Upload Example:**
```dart
class PlantAnalysisScreen extends StatefulWidget {
  @override
  _PlantAnalysisScreenState createState() => _PlantAnalysisScreenState();
}

class _PlantAnalysisScreenState extends State<PlantAnalysisScreen> {
  final MultiImageUploadManager _uploadManager = MultiImageUploadManager();

  // Store base64 data URIs (NOT URLs!)
  String? mainImageDataUri;
  String? leafTopDataUri;
  String? leafBottomDataUri;

  Future<void> captureMainImage() async {
    final file = await ImageCaptureHelper().captureImage(
      source: ImageSource.camera,
    );

    if (file != null) {
      final dataUri = await _uploadManager.processAndStoreImage(file);
      setState(() {
        mainImageDataUri = dataUri;  // Base64 data URI
      });
    }
  }

  Future<void> submitAnalysis() async {
    if (mainImageDataUri == null) {
      // Show error
      return;
    }

    final request = MultiImagePlantAnalysisRequest(
      image: mainImageDataUri!,              // Base64 data URI
      leafTopImage: leafTopDataUri,          // Base64 data URI or null
      leafBottomImage: leafBottomDataUri,    // Base64 data URI or null
      cropType: 'Tomato',
      location: 'Greenhouse A1',
    );

    try {
      final response = await apiService.submitMultiImageAnalysis(request);
      // Handle response...
    } finally {
      // Cleanup temp files after upload
      _uploadManager.cleanup();
    }
  }

  @override
  void dispose() {
    _uploadManager.cleanup();
    super.dispose();
  }
}
```

---

## Error Handling

### Common Errors & User Messages

```dart
class AnalysisErrorHandler {
  static String getUserFriendlyMessage(dynamic error) {
    if (error is DioException) {
      switch (error.response?.statusCode) {
        case 400:
          return 'Invalid image format. Please use JPEG or PNG images.';

        case 401:
          return 'Your session has expired. Please login again.';

        case 403:
          final message = error.response?.data['message'] as String?;
          if (message?.contains('quota') ?? false) {
            return 'Daily analysis limit reached. Upgrade your subscription for more analyses.';
          }
          return 'You don\'t have permission to perform this analysis.';

        case 503:
          return 'Analysis service is temporarily unavailable. Please try again in a few minutes.';

        default:
          return 'Failed to submit analysis. Please check your internet connection.';
      }
    }

    if (error is QuotaExceededException) {
      return '${error.message}\n\nWould you like to upgrade your subscription?';
    }

    if (error is QueueUnavailableException) {
      return 'Analysis service is currently busy. Please try again in a few minutes.';
    }

    return 'An unexpected error occurred. Please try again.';
  }

  static bool shouldRetry(dynamic error) {
    if (error is DioException) {
      final statusCode = error.response?.statusCode;
      // Retry on 503 (service unavailable) and 429 (rate limit)
      return statusCode == 503 || statusCode == 429;
    }

    if (error is QueueUnavailableException) {
      return true;
    }

    return false;
  }

  static bool shouldShowUpgradePrompt(dynamic error) {
    if (error is DioException && error.response?.statusCode == 403) {
      final message = error.response?.data['message'] as String?;
      return message?.toLowerCase().contains('quota') ?? false;
    }

    if (error is QuotaExceededException) {
      return true;
    }

    return false;
  }
}

// Custom exceptions
class QuotaExceededException implements Exception {
  final String message;
  QuotaExceededException(this.message);
}

class QueueUnavailableException implements Exception {
  final String message;
  QueueUnavailableException(this.message);
}
```

### Retry Logic

```dart
class RetryHelper {
  static Future<T> retryWithExponentialBackoff<T>({
    required Future<T> Function() operation,
    int maxAttempts = 3,
    Duration initialDelay = const Duration(seconds: 2),
  }) async {
    int attempts = 0;
    Duration delay = initialDelay;

    while (true) {
      try {
        return await operation();
      } catch (e) {
        attempts++;

        if (attempts >= maxAttempts || !AnalysisErrorHandler.shouldRetry(e)) {
          rethrow;
        }

        debugPrint('Retry attempt $attempts after ${delay.inSeconds}s delay');
        await Future.delayed(delay);
        delay *= 2; // Exponential backoff
      }
    }
  }
}

// Usage:
final response = await RetryHelper.retryWithExponentialBackoff(
  operation: () => _apiService.submitMultiImageAnalysis(request),
  maxAttempts: 3,
);
```

---

## Testing Checklist

### Functional Testing

- [ ] **Single Image Upload**
  - [ ] Take photo with camera
  - [ ] Select from gallery
  - [ ] Submit successfully
  - [ ] Receive analysisId
  - [ ] Poll for results
  - [ ] Display results correctly

- [ ] **Multi-Image Upload (2-5 images)**
  - [ ] Select main image (required)
  - [ ] Add optional images (leaf top, bottom, overview, root)
  - [ ] Preview all selected images
  - [ ] Remove and re-add images
  - [ ] Submit with 2, 3, 4, and 5 images
  - [ ] Receive analysisId with imageUrls
  - [ ] Poll for results
  - [ ] Display all image URLs in results

- [ ] **Mixed Scenarios**
  - [ ] Switch between single and multi-image modes
  - [ ] Submit single-image, then multi-image
  - [ ] View history with both types

### Error Handling Testing

- [ ] **Network Errors**
  - [ ] No internet connection
  - [ ] Slow/timeout
  - [ ] Server error (503)
  - [ ] Display appropriate error messages
  - [ ] Retry logic works

- [ ] **Validation Errors**
  - [ ] Invalid image format
  - [ ] No main image provided
  - [ ] Image too large (pre-optimization)
  - [ ] Display field-specific errors

- [ ] **Quota Errors**
  - [ ] Daily limit reached (403)
  - [ ] Monthly limit reached (403)
  - [ ] Display upgrade prompt
  - [ ] Link to subscription page

- [ ] **Queue Errors**
  - [ ] Queue health check fails
  - [ ] Queue unavailable (503)
  - [ ] Polling timeout
  - [ ] Display appropriate messages

### UI/UX Testing

- [ ] **Image Selection UI**
  - [ ] Clear labels for each image type
  - [ ] Tips/instructions visible
  - [ ] Preview thumbnails correct
  - [ ] Remove button works

- [ ] **Upload Progress**
  - [ ] Progress indicators per image
  - [ ] Overall progress visible
  - [ ] Cancel option works

- [ ] **Results Display**
  - [ ] All images displayed in gallery
  - [ ] Swipe navigation works
  - [ ] Image labels correct
  - [ ] Health score and analysis data correct

### Performance Testing

- [ ] **Memory Usage**
  - [ ] Upload 5 large images (5MB each)
  - [ ] No memory leaks
  - [ ] Temp files cleaned up

- [ ] **Upload Speed**
  - [ ] 5 optimized images upload in <30 seconds
  - [ ] Progress feedback responsive

- [ ] **Results Polling**
  - [ ] Polling doesn't block UI
  - [ ] Max timeout (100s) respected
  - [ ] Results display immediately when available

### Edge Cases

- [ ] **Interrupted Uploads**
  - [ ] App backgrounded during upload
  - [ ] Network switch during upload
  - [ ] App killed during upload

- [ ] **Quota Edge Cases**
  - [ ] Submit when at quota limit
  - [ ] Submit after quota reset (midnight UTC)

- [ ] **Data Consistency**
  - [ ] Analysis ID matches between submit and results
  - [ ] Image URLs accessible
  - [ ] Multi-image flag correct (isMultiImageAnalysis())

---

## Migration Path

### Phase 1: Preparation (Week 1)
1. Update data models (add multi-image fields)
2. Update API service (add multi-image methods)
3. Add error handling for new cases
4. Unit test new models and services

### Phase 2: UI Implementation (Week 2)
1. Create multi-image selection screen
2. Update image capture/selection logic
3. Implement upload progress UI
4. Add image gallery to results screen

### Phase 3: Integration & Testing (Week 3)
1. Integrate with existing analysis flow
2. End-to-end testing (single and multi)
3. Performance testing (memory, speed)
4. User acceptance testing

### Phase 4: Release (Week 4)
1. Beta release to select users
2. Monitor analytics and errors
3. Gather feedback
4. Full production release

---

## Questions & Support

### Common Questions

**Q: Do I need to change existing single-image code?**
A: No, keep existing single-image implementation unchanged. Add multi-image as a separate flow.

**Q: Are multi-image analyses more expensive (quota-wise)?**
A: No, 1 multi-image analysis = 1 request, same as single-image.

**Q: What if user uploads only 1 image via multi-image endpoint?**
A: It works fine - just provide the main image and leave optionals null.

**Q: Can I use the same polling logic for both?**
A: Yes, GET /{analysisId} works for both types. Response includes multi-image URLs if available.

**Q: How do I know if a result is from multi-image analysis?**
A: Check `result.isMultiImageAnalysis()` or if any of the optional URL fields are non-null.

### Contact

**Backend Team:**
- Technical questions: backend@ziraai.com
- API issues: api-support@ziraai.com

**Mobile Team Lead:**
- Integration support: mobile-lead@ziraai.com

**Documentation:**
- API Docs: [multi-image-analysis-api-documentation.md](./multi-image-analysis-api-documentation.md)
- Postman Collection: [multi-image-analysis-postman-collection.json](./multi-image-analysis-postman-collection.json)

---

## Changelog

### Version 1.0 (2025-02-27)
- Initial multi-image integration guide
- Complete Flutter code examples
- Testing checklist and migration path
