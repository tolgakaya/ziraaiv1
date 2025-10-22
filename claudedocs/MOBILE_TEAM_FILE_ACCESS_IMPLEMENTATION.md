# Mobile Team Implementation Guide - Secure File Access

## 🎯 Özet (Summary)

Voice message ve attachment dosyalarına erişim artık **güvenli controller endpoint'leri** üzerinden yapılıyor. Eski fiziksel URL'ler yerine API endpoint URL'leri kullanılacak.

### ✅ Ne Değişti?

**ESKİ YÖNTEM (Artık Kullanılmıyor):**
```
https://ziraai-api-sit.up.railway.app/voice-messages/voice_msg_165_638965887035374118_1760991903.m4a
```
- ❌ Public erişim
- ❌ Authorization yok
- ❌ Güvenlik sorunu

**YENİ YÖNTEM (Kullanılması Gereken):**
```
https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/165
```
- ✅ JWT authentication gerekli
- ✅ Sadece mesaj katılımcıları erişebilir
- ✅ Güvenli ve audit edilebilir

---

## 📋 API Endpoint Değişiklikleri

### 1. Conversation Endpoint Response Değişikliği

**Endpoint:** `GET /api/v1/sponsorship/messages/conversation`

**Request:**
```http
GET /api/v1/sponsorship/messages/conversation?fromUserId=123&toUserId=456&plantAnalysisId=789&page=1&pageSize=20
Authorization: Bearer {jwt_token}
x-dev-arch-version: 1
```

#### Response - Voice Message Örneği

**ÖNCEKİ Response (Eski):**
```json
{
  "data": [
    {
      "id": 165,
      "plantAnalysisId": 789,
      "fromUserId": 123,
      "toUserId": 456,
      "message": "[Voice Message]",
      "messageType": "VoiceMessage",

      "isVoiceMessage": true,
      "voiceMessageUrl": "https://ziraai-api-sit.up.railway.app/voice-messages/voice_msg_165_638965887035374118_1760991903.m4a",
      "voiceMessageDuration": 45,
      "voiceMessageWaveform": "[0.2,0.5,0.8,...]"
    }
  ],
  "success": true
}
```

**YENİ Response (Güncel):**
```json
{
  "data": [
    {
      "id": 165,
      "plantAnalysisId": 789,
      "fromUserId": 123,
      "toUserId": 456,
      "message": "[Voice Message]",
      "messageType": "VoiceMessage",
      "messageStatus": "Sent",
      "isRead": false,
      "sentDate": "2025-10-21T10:30:00Z",
      "deliveredDate": null,
      "readDate": null,

      "senderRole": "Sponsor",
      "senderName": "Ahmet Yılmaz",
      "senderCompany": "Yılmaz Tarım A.Ş.",
      "senderAvatarUrl": "https://...",
      "senderAvatarThumbnailUrl": "https://...",

      "receiverName": "Mehmet Demir",
      "receiverAvatarUrl": "https://...",
      "receiverAvatarThumbnailUrl": "https://...",

      "priority": "Normal",
      "category": "General",

      "hasAttachments": false,
      "attachmentCount": 0,
      "attachmentUrls": null,

      "isVoiceMessage": true,
      "voiceMessageUrl": "https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/165",
      "voiceMessageDuration": 45,
      "voiceMessageWaveform": "[0.2,0.5,0.8,0.6,0.4,0.7,0.9,0.5,0.3,0.1]",

      "isEdited": false,
      "editedDate": null,
      "isForwarded": false,
      "forwardedFromMessageId": null,
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "success": true
}
```

#### 🔑 Önemli Değişiklikler:

1. **voiceMessageUrl** artık API endpoint formatında:
   ```
   https://ziraai-api-sit.up.railway.app/api/v1/files/voice-messages/{messageId}
   ```

2. **messageId** kullanılarak dosyaya erişim sağlanıyor (URL'de `165`)

3. **JWT token** ile authentication zorunlu

---

#### Response - Attachment Örneği

**YENİ Response (Güncel):**
```json
{
  "data": [
    {
      "id": 167,
      "plantAnalysisId": 789,
      "fromUserId": 123,
      "toUserId": 456,
      "message": "Fotoğrafları inceleyebilir misiniz?",
      "messageType": "Text",
      "messageStatus": "Delivered",
      "isRead": false,
      "sentDate": "2025-10-21T11:15:00Z",
      "deliveredDate": "2025-10-21T11:15:05Z",
      "readDate": null,

      "senderRole": "Farmer",
      "senderName": "Mehmet Demir",
      "senderCompany": "",
      "senderAvatarUrl": "https://...",
      "senderAvatarThumbnailUrl": "https://...",

      "priority": "Normal",
      "category": "General",

      "hasAttachments": true,
      "attachmentCount": 3,
      "attachmentUrls": [
        "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/167/0",
        "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/167/1",
        "https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/167/2"
      ],
      "attachmentTypes": [
        "image/jpeg",
        "image/jpeg",
        "application/pdf"
      ],
      "attachmentSizes": [
        245678,
        198432,
        512000
      ],
      "attachmentNames": [
        "bitki_foto_1.jpg",
        "bitki_foto_2.jpg",
        "analiz_raporu.pdf"
      ],

      "isVoiceMessage": false,
      "voiceMessageUrl": null,
      "voiceMessageDuration": null,
      "voiceMessageWaveform": null,

      "isEdited": false,
      "isForwarded": false,
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalRecords": 45,
  "totalPages": 3,
  "success": true
}
```

#### 🔑 Attachment URL Formatı:

```
https://ziraai-api-sit.up.railway.app/api/v1/files/attachments/{messageId}/{attachmentIndex}
```

- **messageId**: Mesajın ID'si (örnek: `167`)
- **attachmentIndex**: 0-based index (ilk dosya: `0`, ikinci dosya: `1`, vb.)

**Örnek:**
- İlk fotoğraf: `.../attachments/167/0`
- İkinci fotoğraf: `.../attachments/167/1`
- PDF dosyası: `.../attachments/167/2`

---

## 🎵 Voice Message Playback Implementation

### Flutter Implementation

#### 1. Voice Message Player Widget

```dart
import 'package:audioplayers/audioplayers.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

class VoiceMessagePlayer extends StatefulWidget {
  final int messageId;
  final int duration; // seconds
  final String waveform; // JSON string
  final String baseUrl;
  final String jwtToken;

  const VoiceMessagePlayer({
    Key? key,
    required this.messageId,
    required this.duration,
    required this.waveform,
    required this.baseUrl,
    required this.jwtToken,
  }) : super(key: key);

  @override
  _VoiceMessagePlayerState createState() => _VoiceMessagePlayerState();
}

class _VoiceMessagePlayerState extends State<VoiceMessagePlayer> {
  final AudioPlayer _audioPlayer = AudioPlayer();
  bool _isPlaying = false;
  Duration _currentPosition = Duration.zero;
  Duration _totalDuration = Duration.zero;
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _setupAudioPlayer();
  }

  void _setupAudioPlayer() {
    _audioPlayer.onPlayerStateChanged.listen((state) {
      if (mounted) {
        setState(() {
          _isPlaying = state == PlayerState.playing;
        });
      }
    });

    _audioPlayer.onDurationChanged.listen((duration) {
      if (mounted) {
        setState(() {
          _totalDuration = duration;
        });
      }
    });

    _audioPlayer.onPositionChanged.listen((position) {
      if (mounted) {
        setState(() {
          _currentPosition = position;
        });
      }
    });
  }

  Future<void> _playPause() async {
    if (_isPlaying) {
      await _audioPlayer.pause();
    } else {
      setState(() {
        _isLoading = true;
      });

      try {
        // ✅ IMPORTANT: Voice message URL with JWT authentication
        final url = '${widget.baseUrl}/api/v1/files/voice-messages/${widget.messageId}';

        await _audioPlayer.play(
          UrlSource(url),
          headers: {
            'Authorization': 'Bearer ${widget.jwtToken}',
          },
        );
      } catch (e) {
        print('Error playing voice message: $e');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Ses mesajı oynatılamadı: $e')),
        );
      } finally {
        setState(() {
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _seekTo(Duration position) async {
    await _audioPlayer.seek(position);
  }

  @override
  void dispose() {
    _audioPlayer.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.blue.shade50,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Row(
        children: [
          // Play/Pause Button
          IconButton(
            icon: _isLoading
                ? SizedBox(
                    width: 24,
                    height: 24,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Icon(_isPlaying ? Icons.pause : Icons.play_arrow),
            onPressed: _isLoading ? null : _playPause,
            color: Colors.blue,
          ),

          // Waveform & Progress Slider
          Expanded(
            child: Column(
              children: [
                // Waveform visualization (optional)
                if (widget.waveform.isNotEmpty)
                  WaveformWidget(
                    waveform: widget.waveform,
                    progress: _totalDuration.inMilliseconds > 0
                        ? _currentPosition.inMilliseconds / _totalDuration.inMilliseconds
                        : 0.0,
                  ),

                // Progress Slider
                Slider(
                  value: _currentPosition.inSeconds.toDouble(),
                  max: widget.duration.toDouble(),
                  onChanged: (value) {
                    _seekTo(Duration(seconds: value.toInt()));
                  },
                ),
              ],
            ),
          ),

          // Duration
          Text(
            '${_formatDuration(_currentPosition)} / ${_formatDuration(Duration(seconds: widget.duration))}',
            style: TextStyle(fontSize: 12, color: Colors.grey),
          ),
        ],
      ),
    );
  }

  String _formatDuration(Duration duration) {
    String twoDigits(int n) => n.toString().padLeft(2, '0');
    final minutes = twoDigits(duration.inMinutes.remainder(60));
    final seconds = twoDigits(duration.inSeconds.remainder(60));
    return '$minutes:$seconds';
  }
}
```

#### 2. Waveform Widget (Optional)

```dart
import 'dart:convert';
import 'package:flutter/material.dart';

class WaveformWidget extends StatelessWidget {
  final String waveform; // JSON array string
  final double progress; // 0.0 to 1.0

  const WaveformWidget({
    Key? key,
    required this.waveform,
    required this.progress,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    List<double> waveformData = [];
    try {
      final parsed = jsonDecode(waveform);
      waveformData = List<double>.from(parsed);
    } catch (e) {
      print('Error parsing waveform: $e');
    }

    return Container(
      height: 40,
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: waveformData.asMap().entries.map((entry) {
          final index = entry.key;
          final amplitude = entry.value;
          final isPlayed = (index / waveformData.length) <= progress;

          return Container(
            width: 3,
            height: 40 * amplitude,
            decoration: BoxDecoration(
              color: isPlayed ? Colors.blue : Colors.grey.shade300,
              borderRadius: BorderRadius.circular(2),
            ),
          );
        }).toList(),
      ),
    );
  }
}
```

#### 3. Usage in Message List

```dart
Widget buildMessageItem(AnalysisMessageDto message) {
  if (message.isVoiceMessage && message.voiceMessageUrl != null) {
    return VoiceMessagePlayer(
      messageId: message.id,
      duration: message.voiceMessageDuration ?? 0,
      waveform: message.voiceMessageWaveform ?? '[]',
      baseUrl: 'https://ziraai-api-sit.up.railway.app',
      jwtToken: _authService.getToken(), // Your auth service
    );
  }

  // Regular text message
  return Text(message.message);
}
```

---

## 📎 Attachment Display Implementation

### Flutter Implementation

#### 1. Attachment Image Display

```dart
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';

class AttachmentImageWidget extends StatelessWidget {
  final String imageUrl;
  final String jwtToken;
  final String? fileName;

  const AttachmentImageWidget({
    Key? key,
    required this.imageUrl,
    required this.jwtToken,
    this.fileName,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () {
        // Open full screen image viewer
        Navigator.push(
          context,
          MaterialPageRoute(
            builder: (context) => FullScreenImageViewer(
              imageUrl: imageUrl,
              jwtToken: jwtToken,
              fileName: fileName,
            ),
          ),
        );
      },
      child: CachedNetworkImage(
        imageUrl: imageUrl,
        httpHeaders: {
          'Authorization': 'Bearer $jwtToken',
        },
        placeholder: (context, url) => Container(
          width: 200,
          height: 200,
          child: Center(child: CircularProgressIndicator()),
        ),
        errorWidget: (context, url, error) => Container(
          width: 200,
          height: 200,
          color: Colors.grey.shade200,
          child: Icon(Icons.error, color: Colors.red),
        ),
        fit: BoxFit.cover,
        width: 200,
        height: 200,
      ),
    );
  }
}
```

#### 2. Attachment Download

```dart
import 'package:dio/dio.dart';
import 'package:path_provider/path_provider.dart';
import 'package:permission_handler/permission_handler.dart';
import 'dart:io';

class AttachmentService {
  final Dio _dio = Dio();

  Future<void> downloadAttachment({
    required String attachmentUrl,
    required String jwtToken,
    required String fileName,
    required Function(double) onProgress,
    required Function(String) onSuccess,
    required Function(String) onError,
  }) async {
    try {
      // Request storage permission
      if (Platform.isAndroid) {
        final status = await Permission.storage.request();
        if (!status.isGranted) {
          onError('Depolama izni gerekli');
          return;
        }
      }

      // Get download directory
      final dir = Platform.isAndroid
          ? await getExternalStorageDirectory()
          : await getApplicationDocumentsDirectory();

      final savePath = '${dir!.path}/$fileName';

      // Download with JWT authentication
      await _dio.download(
        attachmentUrl,
        savePath,
        options: Options(
          headers: {
            'Authorization': 'Bearer $jwtToken',
          },
        ),
        onReceiveProgress: (received, total) {
          if (total != -1) {
            final progress = received / total;
            onProgress(progress);
          }
        },
      );

      onSuccess(savePath);
    } catch (e) {
      onError('İndirme hatası: $e');
    }
  }
}
```

#### 3. Usage in Message List - Attachments

```dart
Widget buildMessageItem(AnalysisMessageDto message) {
  if (message.hasAttachments && message.attachmentUrls != null) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Message text
        if (message.message.isNotEmpty)
          Text(message.message),

        SizedBox(height: 8),

        // Attachments
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: List.generate(message.attachmentCount, (index) {
            final url = message.attachmentUrls![index];
            final type = message.attachmentTypes?[index] ?? '';
            final name = message.attachmentNames?[index] ?? 'file_$index';
            final size = message.attachmentSizes?[index] ?? 0;

            if (type.startsWith('image/')) {
              // Image attachment
              return AttachmentImageWidget(
                imageUrl: url,
                jwtToken: _authService.getToken(),
                fileName: name,
              );
            } else {
              // Other file types (PDF, documents, etc.)
              return AttachmentFileWidget(
                fileUrl: url,
                fileName: name,
                fileSize: size,
                fileType: type,
                jwtToken: _authService.getToken(),
              );
            }
          }),
        ),
      ],
    );
  }

  // Regular text message
  return Text(message.message);
}
```

#### 4. File Attachment Widget (PDF, Documents)

```dart
class AttachmentFileWidget extends StatelessWidget {
  final String fileUrl;
  final String fileName;
  final int fileSize;
  final String fileType;
  final String jwtToken;

  const AttachmentFileWidget({
    Key? key,
    required this.fileUrl,
    required this.fileName,
    required this.fileSize,
    required this.fileType,
    required this.jwtToken,
  }) : super(key: key);

  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }

  IconData _getFileIcon() {
    if (fileType.contains('pdf')) return Icons.picture_as_pdf;
    if (fileType.contains('word') || fileType.contains('document')) return Icons.description;
    if (fileType.contains('excel') || fileType.contains('spreadsheet')) return Icons.table_chart;
    return Icons.insert_drive_file;
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey.shade300),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(_getFileIcon(), color: Colors.blue, size: 32),
          SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                fileName,
                style: TextStyle(fontWeight: FontWeight.w500),
              ),
              Text(
                _formatFileSize(fileSize),
                style: TextStyle(fontSize: 12, color: Colors.grey),
              ),
            ],
          ),
          SizedBox(width: 12),
          IconButton(
            icon: Icon(Icons.download),
            onPressed: () {
              _downloadFile(context);
            },
          ),
        ],
      ),
    );
  }

  Future<void> _downloadFile(BuildContext context) async {
    final attachmentService = AttachmentService();

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) {
        double progress = 0.0;
        return StatefulBuilder(
          builder: (context, setState) {
            return AlertDialog(
              title: Text('İndiriliyor...'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  LinearProgressIndicator(value: progress),
                  SizedBox(height: 16),
                  Text('${(progress * 100).toStringAsFixed(0)}%'),
                ],
              ),
            );
          },
        );
      },
    );

    await attachmentService.downloadAttachment(
      attachmentUrl: fileUrl,
      jwtToken: jwtToken,
      fileName: fileName,
      onProgress: (p) {
        // Update progress
      },
      onSuccess: (path) {
        Navigator.pop(context); // Close progress dialog
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Dosya indirildi: $path')),
        );
      },
      onError: (error) {
        Navigator.pop(context); // Close progress dialog
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error), backgroundColor: Colors.red),
        );
      },
    );
  }
}
```

---

## 🔐 Authentication & Error Handling

### 1. JWT Token Management

```dart
class AuthService {
  String? _jwtToken;

  void setToken(String token) {
    _jwtToken = token;
  }

  String getToken() {
    if (_jwtToken == null || _jwtToken!.isEmpty) {
      throw Exception('JWT token not found. Please login again.');
    }
    return _jwtToken!;
  }

  void clearToken() {
    _jwtToken = null;
  }
}
```

### 2. Error Handling

```dart
Future<void> handleFileAccess({
  required String url,
  required String jwtToken,
  required Function onSuccess,
  required Function(String) onError,
}) async {
  try {
    final response = await http.get(
      Uri.parse(url),
      headers: {
        'Authorization': 'Bearer $jwtToken',
      },
    );

    if (response.statusCode == 200) {
      onSuccess();
    } else if (response.statusCode == 401) {
      onError('Oturum süreniz doldu. Lütfen tekrar giriş yapın.');
      // Redirect to login
    } else if (response.statusCode == 403) {
      onError('Bu dosyaya erişim yetkiniz yok.');
    } else if (response.statusCode == 404) {
      onError('Dosya bulunamadı.');
    } else {
      onError('Bir hata oluştu (${response.statusCode})');
    }
  } catch (e) {
    onError('Ağ hatası: $e');
  }
}
```

---

## 📊 Complete Example - Message Model

```dart
class AnalysisMessageDto {
  final int id;
  final int plantAnalysisId;
  final int fromUserId;
  final int toUserId;
  final String message;
  final String messageType;

  // Status
  final String messageStatus;
  final bool isRead;
  final DateTime sentDate;
  final DateTime? deliveredDate;
  final DateTime? readDate;

  // Sender info
  final String? senderRole;
  final String? senderName;
  final String? senderCompany;
  final String? senderAvatarUrl;
  final String? senderAvatarThumbnailUrl;

  // Receiver info
  final String? receiverName;
  final String? receiverAvatarUrl;
  final String? receiverAvatarThumbnailUrl;

  // Attachments
  final bool hasAttachments;
  final int attachmentCount;
  final List<String>? attachmentUrls;
  final List<String>? attachmentTypes;
  final List<int>? attachmentSizes;
  final List<String>? attachmentNames;

  // Voice message
  final bool isVoiceMessage;
  final String? voiceMessageUrl;
  final int? voiceMessageDuration;
  final String? voiceMessageWaveform;

  AnalysisMessageDto({
    required this.id,
    required this.plantAnalysisId,
    required this.fromUserId,
    required this.toUserId,
    required this.message,
    required this.messageType,
    required this.messageStatus,
    required this.isRead,
    required this.sentDate,
    this.deliveredDate,
    this.readDate,
    this.senderRole,
    this.senderName,
    this.senderCompany,
    this.senderAvatarUrl,
    this.senderAvatarThumbnailUrl,
    this.receiverName,
    this.receiverAvatarUrl,
    this.receiverAvatarThumbnailUrl,
    required this.hasAttachments,
    required this.attachmentCount,
    this.attachmentUrls,
    this.attachmentTypes,
    this.attachmentSizes,
    this.attachmentNames,
    required this.isVoiceMessage,
    this.voiceMessageUrl,
    this.voiceMessageDuration,
    this.voiceMessageWaveform,
  });

  factory AnalysisMessageDto.fromJson(Map<String, dynamic> json) {
    return AnalysisMessageDto(
      id: json['id'],
      plantAnalysisId: json['plantAnalysisId'],
      fromUserId: json['fromUserId'],
      toUserId: json['toUserId'],
      message: json['message'],
      messageType: json['messageType'],
      messageStatus: json['messageStatus'],
      isRead: json['isRead'],
      sentDate: DateTime.parse(json['sentDate']),
      deliveredDate: json['deliveredDate'] != null
          ? DateTime.parse(json['deliveredDate'])
          : null,
      readDate: json['readDate'] != null
          ? DateTime.parse(json['readDate'])
          : null,
      senderRole: json['senderRole'],
      senderName: json['senderName'],
      senderCompany: json['senderCompany'],
      senderAvatarUrl: json['senderAvatarUrl'],
      senderAvatarThumbnailUrl: json['senderAvatarThumbnailUrl'],
      receiverName: json['receiverName'],
      receiverAvatarUrl: json['receiverAvatarUrl'],
      receiverAvatarThumbnailUrl: json['receiverAvatarThumbnailUrl'],
      hasAttachments: json['hasAttachments'],
      attachmentCount: json['attachmentCount'],
      attachmentUrls: json['attachmentUrls'] != null
          ? List<String>.from(json['attachmentUrls'])
          : null,
      attachmentTypes: json['attachmentTypes'] != null
          ? List<String>.from(json['attachmentTypes'])
          : null,
      attachmentSizes: json['attachmentSizes'] != null
          ? List<int>.from(json['attachmentSizes'])
          : null,
      attachmentNames: json['attachmentNames'] != null
          ? List<String>.from(json['attachmentNames'])
          : null,
      isVoiceMessage: json['isVoiceMessage'],
      voiceMessageUrl: json['voiceMessageUrl'],
      voiceMessageDuration: json['voiceMessageDuration'],
      voiceMessageWaveform: json['voiceMessageWaveform'],
    );
  }
}
```

---

## 🧪 Test Scenarios

### Test 1: Voice Message Playback

**Adımlar:**
1. Conversation endpoint'inden mesajları al
2. `isVoiceMessage: true` olan mesajı bul
3. `voiceMessageUrl` değerini kullanarak audio player'a yükle
4. **Mutlaka JWT token header'ında gönder**
5. Ses oynatmayı test et
6. İleri-geri sarma (seek) işlevini test et

**Beklenen Sonuç:**
- ✅ Ses dosyası başarıyla yüklenir
- ✅ Oynatma düzgün çalışır
- ✅ Seek işlevi çalışır

### Test 2: Attachment Display

**Adımlar:**
1. `hasAttachments: true` olan mesajı bul
2. `attachmentUrls` array'inden URL'leri al
3. Her attachment için:
   - Image ise: CachedNetworkImage ile göster
   - Diğer dosyalar için: Download butonu göster
4. **JWT token header'ında gönder**

**Beklenen Sonuç:**
- ✅ Resimler görüntülenir
- ✅ PDF/doküman indirme çalışır

### Test 3: Authorization Failure

**Adımlar:**
1. Yanlış/expired JWT token kullan
2. Voice message URL'e istek gönder

**Beklenen Sonuç:**
- ❌ 401 Unauthorized hatası alınır
- ✅ Kullanıcıya "Oturum süreniz doldu" mesajı gösterilir

### Test 4: Cross-User Access (Security Test)

**Adımlar:**
1. User A olarak login ol
2. User B'nin mesajındaki voice message URL'ine erişmeyi dene

**Beklenen Sonuç:**
- ❌ 403 Forbidden hatası alınır
- ✅ "Erişim yetkiniz yok" mesajı gösterilir

---

## 🚨 Common Issues & Solutions

### Issue 1: "AudioPlayerException: Failed to set source"

**Sebep:** JWT token header'da gönderilmemiş

**Çözüm:**
```dart
await _audioPlayer.play(
  UrlSource(url),
  headers: {
    'Authorization': 'Bearer $jwtToken', // ✅ ZORUNLU
  },
);
```

### Issue 2: 401 Unauthorized

**Sebep:** Token expired veya invalid

**Çözüm:**
1. Token'ı refresh et
2. Kullanıcıyı login sayfasına yönlendir

### Issue 3: 403 Forbidden

**Sebep:** Kullanıcı mesaj katılımcısı değil (security working as expected)

**Çözüm:**
- Normal bir durum, hata mesajı göster
- Kullanıcıya sadece kendi mesajlarına erişebildiğini bildir

### Issue 4: 404 Not Found

**Sebep:**
- Mesaj silinmiş
- Dosya sunucuda yok
- Yanlış messageId

**Çözüm:**
- Kullanıcıya "Dosya bulunamadı" mesajı göster
- Mesaj listesini refresh et

---

## 📝 Checklist for Mobile Team

### ✅ Implementation Checklist

- [ ] **Model Update:** `AnalysisMessageDto` modelini güncelle
- [ ] **Voice Player:** JWT header ile voice message playback implement et
- [ ] **Attachments:** JWT header ile attachment display implement et
- [ ] **Error Handling:** 401, 403, 404 hataları için UI feedback ekle
- [ ] **Token Management:** JWT token refresh mekanizması
- [ ] **Testing:** Tüm test senaryolarını çalıştır
- [ ] **Security Test:** Cross-user access test et (403 almalı)

### 🔍 Testing Checklist

- [ ] Voice message oynatma (iOS)
- [ ] Voice message oynatma (Android)
- [ ] Voice message seek işlevi
- [ ] Image attachment görüntüleme
- [ ] PDF/document download
- [ ] Expired token durumu (401)
- [ ] Unauthorized access (403)
- [ ] Network error handling

---

## 📞 Support & Questions

Sorularınız için:
- Backend team ile iletişime geçin
- Postman collection'da test örnekleri var
- Staging environment: `https://ziraai-api-sit.up.railway.app`

**Important URLs:**
- **Development:** `https://localhost:5001`
- **Staging:** `https://ziraai-api-sit.up.railway.app`
- **Production:** `https://ziraai.com` (deploy sonrası)

---

## 🔄 Migration Timeline

1. **Backend Deploy:** ✅ Completed
2. **Mobile Implementation:** 🔄 In Progress (Your Task)
3. **Testing:** ⏳ Pending
4. **Production Deploy:** ⏳ Pending

İyi çalışmalar! 🚀
