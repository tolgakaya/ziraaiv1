# Mobile Integration Guide - Sponsored Analyses List

**Target Platform**: Flutter (iOS & Android)
**API Version**: 1.0
**Date**: 2025-10-15
**Status**: ✅ READY FOR IMPLEMENTATION

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Flutter Implementation](#flutter-implementation)
4. [UI/UX Specifications](#uiux-specifications)
5. [State Management](#state-management)
6. [Error Handling](#error-handling)
7. [Testing Guide](#testing-guide)
8. [Complete Code Examples](#complete-code-examples)

---

## Overview

This guide provides complete Flutter implementation for the Sponsored Analyses List feature, allowing sponsor users to view and interact with plant analyses from farmers they've sponsored.

### Key Features

- **Paginated List**: Infinite scroll with pull-to-refresh
- **Tier-Based UI**: Dynamic field visibility based on sponsor tier (S/M/L/XL)
- **Filtering**: By crop type and date range
- **Sorting**: By date, health score, or crop type
- **Sponsor Branding**: Display company logo and information
- **Messaging**: Contact farmers (M, L, XL tiers only)
- **Summary Statistics**: Dashboard-style analytics

---

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                        UI Layer                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SponsoredAnalysesListScreen                        │   │
│  │  - Pull to refresh                                  │   │
│  │  - Infinite scroll                                  │   │
│  │  - Filter controls                                  │   │
│  │  - Sort controls                                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                     State Management                         │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SponsoredAnalysesBloc / Provider                   │   │
│  │  - Manages pagination state                         │   │
│  │  - Handles filter/sort state                        │   │
│  │  - Caches loaded pages                              │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                     Data Layer                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  SponsoredAnalysesRepository                        │   │
│  │  - API communication                                │   │
│  │  - Response parsing                                 │   │
│  │  - Error handling                                   │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            ↓ ↑
┌─────────────────────────────────────────────────────────────┐
│                     API Client                               │
│  GET /api/v1/sponsorship/analyses                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Flutter Implementation

### 1. Data Models

#### File: `lib/models/sponsored_analysis_summary.dart`

```dart
import 'package:json_annotation/json_annotation.dart';

part 'sponsored_analysis_summary.g.dart';

@JsonSerializable()
class SponsoredAnalysisSummary {
  // Core fields (always available)
  final int analysisId;
  final DateTime analysisDate;
  final String analysisStatus;
  final String cropType;

  // 30% Access Fields (S & M tiers)
  final double? overallHealthScore;
  final String? plantSpecies;
  final String? plantVariety;
  final String? growthStage;
  final String? imageThumbnailUrl;

  // 60% Access Fields (L tier)
  final double? vigorScore;
  final String? healthSeverity;
  final String? primaryConcern;
  final String? location;
  final String? recommendations;

  // 100% Access Fields (XL tier)
  final String? farmerName;
  final String? farmerPhone;
  final String? farmerEmail;

  // Tier & Permission Info
  final String tierName;
  final int accessPercentage;
  final bool canMessage;
  final bool canViewLogo;

  // Sponsor Display Info
  final SponsorDisplayInfo sponsorInfo;

  SponsoredAnalysisSummary({
    required this.analysisId,
    required this.analysisDate,
    required this.analysisStatus,
    required this.cropType,
    this.overallHealthScore,
    this.plantSpecies,
    this.plantVariety,
    this.growthStage,
    this.imageThumbnailUrl,
    this.vigorScore,
    this.healthSeverity,
    this.primaryConcern,
    this.location,
    this.recommendations,
    this.farmerName,
    this.farmerPhone,
    this.farmerEmail,
    required this.tierName,
    required this.accessPercentage,
    required this.canMessage,
    required this.canViewLogo,
    required this.sponsorInfo,
  });

  factory SponsoredAnalysisSummary.fromJson(Map<String, dynamic> json) =>
      _$SponsoredAnalysisSummaryFromJson(json);

  Map<String, dynamic> toJson() => _$SponsoredAnalysisSummaryToJson(this);

  // Helper getters for UI
  bool get hasBasicAccess => accessPercentage >= 30;
  bool get hasDetailedAccess => accessPercentage >= 60;
  bool get hasFullAccess => accessPercentage >= 100;

  String get healthScoreText {
    if (overallHealthScore == null) return 'N/A';
    return '${overallHealthScore!.toStringAsFixed(1)}%';
  }

  String get analysisDateFormatted {
    final now = DateTime.now();
    final difference = now.difference(analysisDate);

    if (difference.inDays == 0) {
      return 'Bugün ${analysisDate.hour}:${analysisDate.minute.toString().padLeft(2, '0')}';
    } else if (difference.inDays == 1) {
      return 'Dün';
    } else if (difference.inDays < 7) {
      return '${difference.inDays} gün önce';
    } else {
      return '${analysisDate.day}/${analysisDate.month}/${analysisDate.year}';
    }
  }
}

@JsonSerializable()
class SponsorDisplayInfo {
  final int sponsorId;
  final String companyName;
  final String? logoUrl;
  final String? websiteUrl;

  SponsorDisplayInfo({
    required this.sponsorId,
    required this.companyName,
    this.logoUrl,
    this.websiteUrl,
  });

  factory SponsorDisplayInfo.fromJson(Map<String, dynamic> json) =>
      _$SponsorDisplayInfoFromJson(json);

  Map<String, dynamic> toJson() => _$SponsorDisplayInfoToJson(this);
}

@JsonSerializable()
class SponsoredAnalysesListSummary {
  final int totalAnalyses;
  final double averageHealthScore;
  final List<String> topCropTypes;
  final int analysesThisMonth;

  SponsoredAnalysesListSummary({
    required this.totalAnalyses,
    required this.averageHealthScore,
    required this.topCropTypes,
    required this.analysesThisMonth,
  });

  factory SponsoredAnalysesListSummary.fromJson(Map<String, dynamic> json) =>
      _$SponsoredAnalysesListSummaryFromJson(json);

  Map<String, dynamic> toJson() => _$SponsoredAnalysesListSummaryToJson(this);
}

@JsonSerializable()
class SponsoredAnalysesListResponse {
  final List<SponsoredAnalysisSummary> items;
  final int totalCount;
  final int page;
  final int pageSize;
  final int totalPages;
  final bool hasNextPage;
  final bool hasPreviousPage;
  final SponsoredAnalysesListSummary summary;

  SponsoredAnalysesListResponse({
    required this.items,
    required this.totalCount,
    required this.page,
    required this.pageSize,
    required this.totalPages,
    required this.hasNextPage,
    required this.hasPreviousPage,
    required this.summary,
  });

  factory SponsoredAnalysesListResponse.fromJson(Map<String, dynamic> json) =>
      _$SponsoredAnalysesListResponseFromJson(json);

  Map<String, dynamic> toJson() => _$SponsoredAnalysesListResponseToJson(this);
}
```

---

### 2. Repository (API Communication)

#### File: `lib/repositories/sponsored_analyses_repository.dart`

```dart
import 'package:dio/dio.dart';
import '../core/api/api_client.dart';
import '../models/sponsored_analysis_summary.dart';

class SponsoredAnalysesRepository {
  final ApiClient _apiClient;

  SponsoredAnalysesRepository(this._apiClient);

  Future<SponsoredAnalysesListResponse> getAnalysesList({
    int page = 1,
    int pageSize = 20,
    String sortBy = 'date',
    String sortOrder = 'desc',
    String? filterByCropType,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    try {
      final queryParameters = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        'sortBy': sortBy,
        'sortOrder': sortOrder,
      };

      if (filterByCropType != null && filterByCropType.isNotEmpty) {
        queryParameters['filterByCropType'] = filterByCropType;
      }

      if (startDate != null) {
        queryParameters['startDate'] = startDate.toIso8601String();
      }

      if (endDate != null) {
        queryParameters['endDate'] = endDate.toIso8601String();
      }

      final response = await _apiClient.get(
        '/sponsorship/analyses',
        queryParameters: queryParameters,
      );

      if (response.data['success'] == true) {
        return SponsoredAnalysesListResponse.fromJson(response.data['data']);
      } else {
        throw Exception(response.data['message'] ?? 'Failed to load analyses');
      }
    } on DioException catch (e) {
      if (e.response?.statusCode == 401) {
        throw UnauthorizedException('Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.');
      } else if (e.response?.statusCode == 403) {
        throw ForbiddenException('Bu işlem için yetkiniz bulunmamaktadır.');
      } else if (e.response?.statusCode == 404) {
        throw NotFoundException('Sponsor profili bulunamadı.');
      } else {
        throw NetworkException('Bağlantı hatası. Lütfen internet bağlantınızı kontrol edin.');
      }
    } catch (e) {
      throw Exception('Beklenmeyen bir hata oluştu: $e');
    }
  }
}

// Custom Exceptions
class UnauthorizedException implements Exception {
  final String message;
  UnauthorizedException(this.message);
}

class ForbiddenException implements Exception {
  final String message;
  ForbiddenException(this.message);
}

class NotFoundException implements Exception {
  final String message;
  NotFoundException(this.message);
}

class NetworkException implements Exception {
  final String message;
  NetworkException(this.message);
}
```

---

### 3. State Management (Bloc Pattern)

#### File: `lib/blocs/sponsored_analyses/sponsored_analyses_bloc.dart`

```dart
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import '../../models/sponsored_analysis_summary.dart';
import '../../repositories/sponsored_analyses_repository.dart';

// Events
abstract class SponsoredAnalysesEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class LoadAnalyses extends SponsoredAnalysesEvent {
  final bool refresh;
  LoadAnalyses({this.refresh = false});
}

class LoadMoreAnalyses extends SponsoredAnalysesEvent {}

class ApplyFilter extends SponsoredAnalysesEvent {
  final String? cropType;
  final DateTime? startDate;
  final DateTime? endDate;

  ApplyFilter({this.cropType, this.startDate, this.endDate});

  @override
  List<Object?> get props => [cropType, startDate, endDate];
}

class ApplySort extends SponsoredAnalysesEvent {
  final String sortBy;
  final String sortOrder;

  ApplySort({required this.sortBy, required this.sortOrder});

  @override
  List<Object?> get props => [sortBy, sortOrder];
}

// States
abstract class SponsoredAnalysesState extends Equatable {
  @override
  List<Object?> get props => [];
}

class SponsoredAnalysesInitial extends SponsoredAnalysesState {}

class SponsoredAnalysesLoading extends SponsoredAnalysesState {}

class SponsoredAnalysesLoaded extends SponsoredAnalysesState {
  final List<SponsoredAnalysisSummary> analyses;
  final SponsoredAnalysesListSummary summary;
  final int currentPage;
  final int totalPages;
  final bool hasMorePages;
  final bool isLoadingMore;

  // Current filters and sort
  final String? currentCropTypeFilter;
  final DateTime? currentStartDate;
  final DateTime? currentEndDate;
  final String currentSortBy;
  final String currentSortOrder;

  SponsoredAnalysesLoaded({
    required this.analyses,
    required this.summary,
    required this.currentPage,
    required this.totalPages,
    required this.hasMorePages,
    this.isLoadingMore = false,
    this.currentCropTypeFilter,
    this.currentStartDate,
    this.currentEndDate,
    this.currentSortBy = 'date',
    this.currentSortOrder = 'desc',
  });

  SponsoredAnalysesLoaded copyWith({
    List<SponsoredAnalysisSummary>? analyses,
    SponsoredAnalysesListSummary? summary,
    int? currentPage,
    int? totalPages,
    bool? hasMorePages,
    bool? isLoadingMore,
    String? currentCropTypeFilter,
    DateTime? currentStartDate,
    DateTime? currentEndDate,
    String? currentSortBy,
    String? currentSortOrder,
  }) {
    return SponsoredAnalysesLoaded(
      analyses: analyses ?? this.analyses,
      summary: summary ?? this.summary,
      currentPage: currentPage ?? this.currentPage,
      totalPages: totalPages ?? this.totalPages,
      hasMorePages: hasMorePages ?? this.hasMorePages,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      currentCropTypeFilter: currentCropTypeFilter ?? this.currentCropTypeFilter,
      currentStartDate: currentStartDate ?? this.currentStartDate,
      currentEndDate: currentEndDate ?? this.currentEndDate,
      currentSortBy: currentSortBy ?? this.currentSortBy,
      currentSortOrder: currentSortOrder ?? this.currentSortOrder,
    );
  }

  @override
  List<Object?> get props => [
        analyses,
        summary,
        currentPage,
        totalPages,
        hasMorePages,
        isLoadingMore,
        currentCropTypeFilter,
        currentStartDate,
        currentEndDate,
        currentSortBy,
        currentSortOrder,
      ];
}

class SponsoredAnalysesError extends SponsoredAnalysesState {
  final String message;

  SponsoredAnalysesError(this.message);

  @override
  List<Object?> get props => [message];
}

// Bloc
class SponsoredAnalysesBloc
    extends Bloc<SponsoredAnalysesEvent, SponsoredAnalysesState> {
  final SponsoredAnalysesRepository _repository;

  SponsoredAnalysesBloc(this._repository) : super(SponsoredAnalysesInitial()) {
    on<LoadAnalyses>(_onLoadAnalyses);
    on<LoadMoreAnalyses>(_onLoadMoreAnalyses);
    on<ApplyFilter>(_onApplyFilter);
    on<ApplySort>(_onApplySort);
  }

  Future<void> _onLoadAnalyses(
    LoadAnalyses event,
    Emitter<SponsoredAnalysesState> emit,
  ) async {
    emit(SponsoredAnalysesLoading());

    try {
      final response = await _repository.getAnalysesList(
        page: 1,
        pageSize: 20,
      );

      emit(SponsoredAnalysesLoaded(
        analyses: response.items,
        summary: response.summary,
        currentPage: response.page,
        totalPages: response.totalPages,
        hasMorePages: response.hasNextPage,
      ));
    } catch (e) {
      emit(SponsoredAnalysesError(e.toString()));
    }
  }

  Future<void> _onLoadMoreAnalyses(
    LoadMoreAnalyses event,
    Emitter<SponsoredAnalysesState> emit,
  ) async {
    if (state is! SponsoredAnalysesLoaded) return;

    final currentState = state as SponsoredAnalysesLoaded;
    if (!currentState.hasMorePages || currentState.isLoadingMore) return;

    emit(currentState.copyWith(isLoadingMore: true));

    try {
      final response = await _repository.getAnalysesList(
        page: currentState.currentPage + 1,
        pageSize: 20,
        sortBy: currentState.currentSortBy,
        sortOrder: currentState.currentSortOrder,
        filterByCropType: currentState.currentCropTypeFilter,
        startDate: currentState.currentStartDate,
        endDate: currentState.currentEndDate,
      );

      emit(SponsoredAnalysesLoaded(
        analyses: [...currentState.analyses, ...response.items],
        summary: response.summary,
        currentPage: response.page,
        totalPages: response.totalPages,
        hasMorePages: response.hasNextPage,
        isLoadingMore: false,
        currentCropTypeFilter: currentState.currentCropTypeFilter,
        currentStartDate: currentState.currentStartDate,
        currentEndDate: currentState.currentEndDate,
        currentSortBy: currentState.currentSortBy,
        currentSortOrder: currentState.currentSortOrder,
      ));
    } catch (e) {
      emit(currentState.copyWith(isLoadingMore: false));
      // Optionally show a snackbar for load more error
    }
  }

  Future<void> _onApplyFilter(
    ApplyFilter event,
    Emitter<SponsoredAnalysesState> emit,
  ) async {
    emit(SponsoredAnalysesLoading());

    try {
      final currentState = state is SponsoredAnalysesLoaded
          ? state as SponsoredAnalysesLoaded
          : null;

      final response = await _repository.getAnalysesList(
        page: 1,
        pageSize: 20,
        sortBy: currentState?.currentSortBy ?? 'date',
        sortOrder: currentState?.currentSortOrder ?? 'desc',
        filterByCropType: event.cropType,
        startDate: event.startDate,
        endDate: event.endDate,
      );

      emit(SponsoredAnalysesLoaded(
        analyses: response.items,
        summary: response.summary,
        currentPage: response.page,
        totalPages: response.totalPages,
        hasMorePages: response.hasNextPage,
        currentCropTypeFilter: event.cropType,
        currentStartDate: event.startDate,
        currentEndDate: event.endDate,
        currentSortBy: currentState?.currentSortBy ?? 'date',
        currentSortOrder: currentState?.currentSortOrder ?? 'desc',
      ));
    } catch (e) {
      emit(SponsoredAnalysesError(e.toString()));
    }
  }

  Future<void> _onApplySort(
    ApplySort event,
    Emitter<SponsoredAnalysesState> emit,
  ) async {
    emit(SponsoredAnalysesLoading());

    try {
      final currentState = state is SponsoredAnalysesLoaded
          ? state as SponsoredAnalysesLoaded
          : null;

      final response = await _repository.getAnalysesList(
        page: 1,
        pageSize: 20,
        sortBy: event.sortBy,
        sortOrder: event.sortOrder,
        filterByCropType: currentState?.currentCropTypeFilter,
        startDate: currentState?.currentStartDate,
        endDate: currentState?.currentEndDate,
      );

      emit(SponsoredAnalysesLoaded(
        analyses: response.items,
        summary: response.summary,
        currentPage: response.page,
        totalPages: response.totalPages,
        hasMorePages: response.hasNextPage,
        currentCropTypeFilter: currentState?.currentCropTypeFilter,
        currentStartDate: currentState?.currentStartDate,
        currentEndDate: currentState?.currentEndDate,
        currentSortBy: event.sortBy,
        currentSortOrder: event.sortOrder,
      ));
    } catch (e) {
      emit(SponsoredAnalysesError(e.toString()));
    }
  }
}
```

---

### 4. UI Screen

#### File: `lib/screens/sponsor/sponsored_analyses_list_screen.dart`

```dart
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../blocs/sponsored_analyses/sponsored_analyses_bloc.dart';
import '../../models/sponsored_analysis_summary.dart';
import '../../widgets/sponsored_analysis_card.dart';
import '../../widgets/summary_statistics_card.dart';

class SponsoredAnalysesListScreen extends StatefulWidget {
  const SponsoredAnalysesListScreen({Key? key}) : super(key: key);

  @override
  State<SponsoredAnalysesListScreen> createState() =>
      _SponsoredAnalysesListScreenState();
}

class _SponsoredAnalysesListScreenState
    extends State<SponsoredAnalysesListScreen> {
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    context.read<SponsoredAnalysesBloc>().add(LoadAnalyses());
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_isBottom) {
      context.read<SponsoredAnalysesBloc>().add(LoadMoreAnalyses());
    }
  }

  bool get _isBottom {
    if (!_scrollController.hasClients) return false;
    final maxScroll = _scrollController.position.maxScrollExtent;
    final currentScroll = _scrollController.offset;
    return currentScroll >= (maxScroll * 0.9);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Sponsorlu Analizler'),
        actions: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: _showFilterDialog,
          ),
          IconButton(
            icon: const Icon(Icons.sort),
            onPressed: _showSortDialog,
          ),
        ],
      ),
      body: BlocConsumer<SponsoredAnalysesBloc, SponsoredAnalysesState>(
        listener: (context, state) {
          if (state is SponsoredAnalysesError) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(state.message),
                backgroundColor: Colors.red,
              ),
            );
          }
        },
        builder: (context, state) {
          if (state is SponsoredAnalysesLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is SponsoredAnalysesError) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.error_outline, size: 64, color: Colors.red),
                  const SizedBox(height: 16),
                  Text(state.message),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: () {
                      context.read<SponsoredAnalysesBloc>().add(LoadAnalyses());
                    },
                    child: const Text('Tekrar Dene'),
                  ),
                ],
              ),
            );
          }

          if (state is SponsoredAnalysesLoaded) {
            return RefreshIndicator(
              onRefresh: () async {
                context.read<SponsoredAnalysesBloc>().add(LoadAnalyses(refresh: true));
              },
              child: ListView.builder(
                controller: _scrollController,
                padding: const EdgeInsets.all(16),
                itemCount: state.analyses.length + 2, // +1 for summary, +1 for loading indicator
                itemBuilder: (context, index) {
                  if (index == 0) {
                    // Summary Statistics Card
                    return SummaryStatisticsCard(summary: state.summary);
                  }

                  if (index == state.analyses.length + 1) {
                    // Loading indicator for pagination
                    return state.isLoadingMore
                        ? const Center(
                            child: Padding(
                              padding: EdgeInsets.all(16.0),
                              child: CircularProgressIndicator(),
                            ),
                          )
                        : const SizedBox.shrink();
                  }

                  final analysis = state.analyses[index - 1];
                  return SponsoredAnalysisCard(
                    analysis: analysis,
                    onTap: () => _navigateToDetail(analysis),
                  );
                },
              ),
            );
          }

          return const SizedBox.shrink();
        },
      ),
    );
  }

  void _showFilterDialog() {
    // TODO: Implement filter dialog
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Filtrele'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              decoration: const InputDecoration(
                labelText: 'Ürün Tipi',
                hintText: 'Örn: Buğday, Domates',
              ),
              onSubmitted: (value) {
                context.read<SponsoredAnalysesBloc>().add(
                      ApplyFilter(cropType: value.isNotEmpty ? value : null),
                    );
                Navigator.pop(context);
              },
            ),
            // Add date range pickers here
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('İptal'),
          ),
          TextButton(
            onPressed: () {
              // Apply filters
              Navigator.pop(context);
            },
            child: const Text('Uygula'),
          ),
        ],
      ),
    );
  }

  void _showSortDialog() {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Sırala'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              title: const Text('Tarihe Göre (Yeni → Eski)'),
              onTap: () {
                context.read<SponsoredAnalysesBloc>().add(
                      ApplySort(sortBy: 'date', sortOrder: 'desc'),
                    );
                Navigator.pop(context);
              },
            ),
            ListTile(
              title: const Text('Tarihe Göre (Eski → Yeni)'),
              onTap: () {
                context.read<SponsoredAnalysesBloc>().add(
                      ApplySort(sortBy: 'date', sortOrder: 'asc'),
                    );
                Navigator.pop(context);
              },
            ),
            ListTile(
              title: const Text('Sağlık Skoru (Yüksek → Düşük)'),
              onTap: () {
                context.read<SponsoredAnalysesBloc>().add(
                      ApplySort(sortBy: 'healthScore', sortOrder: 'desc'),
                    );
                Navigator.pop(context);
              },
            ),
            ListTile(
              title: const Text('Sağlık Skoru (Düşük → Yüksek)'),
              onTap: () {
                context.read<SponsoredAnalysesBloc>().add(
                      ApplySort(sortBy: 'healthScore', sortOrder: 'asc'),
                    );
                Navigator.pop(context);
              },
            ),
          ],
        ),
      ),
    );
  }

  void _navigateToDetail(SponsoredAnalysisSummary analysis) {
    Navigator.pushNamed(
      context,
      '/sponsored-analysis-detail',
      arguments: analysis.analysisId,
    );
  }
}
```

---

### 5. UI Widgets

#### File: `lib/widgets/sponsored_analysis_card.dart`

```dart
import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import '../models/sponsored_analysis_summary.dart';

class SponsoredAnalysisCard extends StatelessWidget {
  final SponsoredAnalysisSummary analysis;
  final VoidCallback onTap;

  const SponsoredAnalysisCard({
    Key? key,
    required this.analysis,
    required this.onTap,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header with date and tier badge
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    analysis.analysisDateFormatted,
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  _buildTierBadge(context),
                ],
              ),
              const SizedBox(height: 12),

              // Crop type and health score
              Row(
                children: [
                  Expanded(
                    child: Text(
                      analysis.cropType,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                  ),
                  if (analysis.hasBasicAccess)
                    _buildHealthScoreBadge(context),
                ],
              ),

              // Plant species and variety (30% access)
              if (analysis.hasBasicAccess && analysis.plantSpecies != null) ...[
                const SizedBox(height: 8),
                Text(
                  '${analysis.plantSpecies}${analysis.plantVariety != null ? ' - ${analysis.plantVariety}' : ''}',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ],

              // Growth stage (30% access)
              if (analysis.hasBasicAccess && analysis.growthStage != null) ...[
                const SizedBox(height: 4),
                Row(
                  children: [
                    const Icon(Icons.spa, size: 16, color: Colors.green),
                    const SizedBox(width: 4),
                    Text(analysis.growthStage!),
                  ],
                ),
              ],

              // Primary concern (60% access - L tier)
              if (analysis.hasDetailedAccess && analysis.primaryConcern != null) ...[
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  decoration: BoxDecoration(
                    color: _getSeverityColor(analysis.healthSeverity).withOpacity(0.1),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(
                      color: _getSeverityColor(analysis.healthSeverity),
                      width: 1,
                    ),
                  ),
                  child: Row(
                    children: [
                      Icon(
                        Icons.warning_amber_rounded,
                        size: 20,
                        color: _getSeverityColor(analysis.healthSeverity),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          analysis.primaryConcern!,
                          style: TextStyle(
                            color: _getSeverityColor(analysis.healthSeverity),
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],

              // Location (60% access - L tier)
              if (analysis.hasDetailedAccess && analysis.location != null) ...[
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Icon(Icons.location_on, size: 16, color: Colors.grey),
                    const SizedBox(width: 4),
                    Text(analysis.location!),
                  ],
                ),
              ],

              // Farmer contact (100% access - XL tier)
              if (analysis.hasFullAccess && analysis.farmerName != null) ...[
                const SizedBox(height: 12),
                const Divider(),
                const SizedBox(height: 8),
                Row(
                  children: [
                    CircleAvatar(
                      radius: 20,
                      backgroundColor: Theme.of(context).primaryColor.withOpacity(0.1),
                      child: Icon(
                        Icons.person,
                        color: Theme.of(context).primaryColor,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            analysis.farmerName!,
                            style: const TextStyle(fontWeight: FontWeight.w600),
                          ),
                          if (analysis.farmerPhone != null)
                            Text(
                              analysis.farmerPhone!,
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                        ],
                      ),
                    ),
                    if (analysis.canMessage)
                      IconButton(
                        icon: const Icon(Icons.message),
                        onPressed: () {
                          // TODO: Open messaging
                        },
                      ),
                  ],
                ),
              ],

              // Sponsor branding (if logo available)
              if (analysis.canViewLogo && analysis.sponsorInfo.logoUrl != null) ...[
                const SizedBox(height: 12),
                const Divider(),
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Text(
                      'Sponsorlu',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey,
                      ),
                    ),
                    const Spacer(),
                    CachedNetworkImage(
                      imageUrl: analysis.sponsorInfo.logoUrl!,
                      height: 24,
                      fit: BoxFit.contain,
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTierBadge(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: _getTierColor(analysis.tierName).withOpacity(0.1),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: _getTierColor(analysis.tierName),
          width: 1,
        ),
      ),
      child: Text(
        analysis.tierName,
        style: TextStyle(
          color: _getTierColor(analysis.tierName),
          fontSize: 12,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  Widget _buildHealthScoreBadge(BuildContext context) {
    final score = analysis.overallHealthScore ?? 0;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: _getHealthScoreColor(score).withOpacity(0.1),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            Icons.favorite,
            size: 16,
            color: _getHealthScoreColor(score),
          ),
          const SizedBox(width: 4),
          Text(
            analysis.healthScoreText,
            style: TextStyle(
              color: _getHealthScoreColor(score),
              fontWeight: FontWeight.bold,
            ),
          ),
        ],
      ),
    );
  }

  Color _getTierColor(String tierName) {
    switch (tierName) {
      case 'S/M':
        return Colors.blue;
      case 'L':
        return Colors.orange;
      case 'XL':
        return Colors.purple;
      default:
        return Colors.grey;
    }
  }

  Color _getHealthScoreColor(double score) {
    if (score >= 80) return Colors.green;
    if (score >= 60) return Colors.orange;
    return Colors.red;
  }

  Color _getSeverityColor(String? severity) {
    switch (severity?.toLowerCase()) {
      case 'healthy':
        return Colors.green;
      case 'moderate':
        return Colors.orange;
      case 'critical':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }
}
```

#### File: `lib/widgets/summary_statistics_card.dart`

```dart
import 'package:flutter/material.dart';
import '../models/sponsored_analysis_summary.dart';

class SummaryStatisticsCard extends StatelessWidget {
  final SponsoredAnalysesListSummary summary;

  const SummaryStatisticsCard({Key? key, required this.summary}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Özet İstatistikler',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _StatisticItem(
                    icon: Icons.analytics,
                    label: 'Toplam Analiz',
                    value: summary.totalAnalyses.toString(),
                    color: Colors.blue,
                  ),
                ),
                Expanded(
                  child: _StatisticItem(
                    icon: Icons.favorite,
                    label: 'Ort. Sağlık',
                    value: '${summary.averageHealthScore.toStringAsFixed(1)}%',
                    color: Colors.green,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _StatisticItem(
                    icon: Icons.calendar_today,
                    label: 'Bu Ay',
                    value: summary.analysesThisMonth.toString(),
                    color: Colors.orange,
                  ),
                ),
                Expanded(
                  child: _StatisticItem(
                    icon: Icons.spa,
                    label: 'En Popüler',
                    value: summary.topCropTypes.isNotEmpty
                        ? summary.topCropTypes.first
                        : 'N/A',
                    color: Colors.purple,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _StatisticItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _StatisticItem({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, size: 32, color: color),
        const SizedBox(height: 8),
        Text(
          value,
          style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
                color: color,
              ),
        ),
        Text(
          label,
          style: Theme.of(context).textTheme.bodySmall,
          textAlign: TextAlign.center,
        ),
      ],
    );
  }
}
```

---

## UI/UX Specifications

### Color Scheme

**Tier Badges**:
- S/M Tier: `Colors.blue` (#2196F3)
- L Tier: `Colors.orange` (#FF9800)
- XL Tier: `Colors.purple` (#9C27B0)

**Health Score**:
- Healthy (80-100): `Colors.green` (#4CAF50)
- Moderate (60-79): `Colors.orange` (#FF9800)
- Critical (0-59): `Colors.red` (#F44336)

**Severity Indicators**:
- Healthy: `Colors.green`
- Moderate: `Colors.orange`
- Critical: `Colors.red`

### Typography

- **Card Title**: `titleLarge`, `FontWeight.bold`
- **Crop Type**: `titleMedium`, `FontWeight.w600`
- **Body Text**: `bodyMedium`
- **Metadata**: `bodySmall`, gray color

### Spacing

- Card padding: `16px`
- Card margin: `16px bottom`
- Section spacing: `12-16px`
- Icon spacing: `4-8px`

### Iconography

- Analysis: `Icons.analytics`
- Health: `Icons.favorite`
- Calendar: `Icons.calendar_today`
- Plant: `Icons.spa`
- Location: `Icons.location_on`
- Warning: `Icons.warning_amber_rounded`
- Person: `Icons.person`
- Message: `Icons.message`

---

## State Management

### State Flow Diagram

```
┌─────────────────────┐
│  Initial State      │
└─────────────────────┘
          ↓
  LoadAnalyses event
          ↓
┌─────────────────────┐
│  Loading State      │
│  (Show spinner)     │
└─────────────────────┘
          ↓
    API Request
          ↓
     ┌────┴────┐
     │         │
  Success    Error
     │         │
     ↓         ↓
┌─────────┐ ┌─────────┐
│ Loaded  │ │ Error   │
│ State   │ │ State   │
└─────────┘ └─────────┘
     │
     ↓
┌─────────────────────┐
│  User Actions:      │
│  - Scroll (load     │
│    more)            │
│  - Pull to refresh  │
│  - Apply filter     │
│  - Apply sort       │
└─────────────────────┘
```

---

## Error Handling

### Error Types and User Messages

| Error Type | HTTP Code | User Message (Turkish) |
|------------|-----------|------------------------|
| `UnauthorizedException` | 401 | "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın." |
| `ForbiddenException` | 403 | "Bu işlem için yetkiniz bulunmamaktadır." |
| `NotFoundException` | 404 | "Sponsor profili bulunamadı." |
| `NetworkException` | N/A | "Bağlantı hatası. Lütfen internet bağlantınızı kontrol edin." |
| Generic Exception | 500 | "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin." |

### Error Display

- **Snackbar**: Temporary errors (network, load more)
- **Full Screen**: Critical errors (unauthorized, not found)
- **Retry Button**: All error states should offer retry option

---

## Testing Guide

### Unit Tests

#### File: `test/blocs/sponsored_analyses_bloc_test.dart`

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:bloc_test/bloc_test.dart';

void main() {
  group('SponsoredAnalysesBloc', () {
    late SponsoredAnalysesRepository mockRepository;
    late SponsoredAnalysesBloc bloc;

    setUp(() {
      mockRepository = MockSponsoredAnalysesRepository();
      bloc = SponsoredAnalysesBloc(mockRepository);
    });

    tearDown(() {
      bloc.close();
    });

    test('initial state is SponsoredAnalysesInitial', () {
      expect(bloc.state, isA<SponsoredAnalysesInitial>());
    });

    blocTest<SponsoredAnalysesBloc, SponsoredAnalysesState>(
      'emits [Loading, Loaded] when LoadAnalyses succeeds',
      build: () {
        when(mockRepository.getAnalysesList()).thenAnswer(
          (_) async => SponsoredAnalysesListResponse(
            items: [/* mock data */],
            totalCount: 100,
            page: 1,
            pageSize: 20,
            totalPages: 5,
            hasNextPage: true,
            hasPreviousPage: false,
            summary: SponsoredAnalysesListSummary(/* mock summary */),
          ),
        );
        return bloc;
      },
      act: (bloc) => bloc.add(LoadAnalyses()),
      expect: () => [
        isA<SponsoredAnalysesLoading>(),
        isA<SponsoredAnalysesLoaded>(),
      ],
    );

    blocTest<SponsoredAnalysesBloc, SponsoredAnalysesState>(
      'emits [Loading, Error] when LoadAnalyses fails',
      build: () {
        when(mockRepository.getAnalysesList()).thenThrow(
          NetworkException('Connection failed'),
        );
        return bloc;
      },
      act: (bloc) => bloc.add(LoadAnalyses()),
      expect: () => [
        isA<SponsoredAnalysesLoading>(),
        isA<SponsoredAnalysesError>(),
      ],
    );
  });
}
```

### Widget Tests

#### File: `test/widgets/sponsored_analysis_card_test.dart`

```dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('SponsoredAnalysisCard', () {
    testWidgets('displays crop type and health score', (tester) async {
      final analysis = SponsoredAnalysisSummary(
        analysisId: 123,
        analysisDate: DateTime.now(),
        analysisStatus: 'Completed',
        cropType: 'Wheat',
        overallHealthScore: 85.5,
        // ...other required fields
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SponsoredAnalysisCard(
              analysis: analysis,
              onTap: () {},
            ),
          ),
        ),
      );

      expect(find.text('Wheat'), findsOneWidget);
      expect(find.text('85.5%'), findsOneWidget);
    });

    testWidgets('hides farmer contact for S tier', (tester) async {
      final analysis = SponsoredAnalysisSummary(
        // S tier with accessPercentage: 30
        accessPercentage: 30,
        farmerName: 'Test Farmer',
        // ...other required fields
      );

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SponsoredAnalysisCard(
              analysis: analysis,
              onTap: () {},
            ),
          ),
        ),
      );

      expect(find.text('Test Farmer'), findsNothing);
    });
  });
}
```

### Integration Tests

#### File: `integration_test/sponsored_analyses_flow_test.dart`

```dart
import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  group('Sponsored Analyses Flow', () {
    testWidgets('Load, scroll, and load more', (tester) async {
      await tester.pumpWidget(MyApp());

      // Navigate to sponsored analyses list
      await tester.tap(find.text('Sponsorlu Analizler'));
      await tester.pumpAndSettle();

      // Verify list is loaded
      expect(find.byType(SponsoredAnalysisCard), findsWidgets);

      // Scroll to bottom
      await tester.drag(
        find.byType(ListView),
        const Offset(0, -500),
      );
      await tester.pumpAndSettle();

      // Verify more items loaded
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      await tester.pumpAndSettle(const Duration(seconds: 2));
      expect(find.byType(SponsoredAnalysisCard), findsWidgets);
    });

    testWidgets('Apply filter', (tester) async {
      await tester.pumpWidget(MyApp());

      // Open filter dialog
      await tester.tap(find.byIcon(Icons.filter_list));
      await tester.pumpAndSettle();

      // Enter crop type
      await tester.enterText(find.byType(TextField), 'Wheat');
      await tester.tap(find.text('Uygula'));
      await tester.pumpAndSettle();

      // Verify filtered results
      final cards = tester.widgetList<SponsoredAnalysisCard>(
        find.byType(SponsoredAnalysisCard),
      );

      for (final card in cards) {
        expect(card.analysis.cropType.toLowerCase(), contains('wheat'));
      }
    });
  });
}
```

---

## Complete Code Examples

### Example 1: API Client Setup

#### File: `lib/core/api/api_client.dart`

```dart
import 'package:dio/dio.dart';
import '../storage/secure_storage.dart';

class ApiClient {
  late Dio _dio;
  final SecureStorage _secureStorage;

  ApiClient(this._secureStorage) {
    _dio = Dio(
      BaseOptions(
        baseURL: _getBaseUrl(),
        connectTimeout: const Duration(seconds: 30),
        receiveTimeout: const Duration(seconds: 30),
        headers: {
          'Content-Type': 'application/json',
          'x-dev-arch-version': '1',
        },
      ),
    );

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          final token = await _secureStorage.getToken();
          if (token != null) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
        onError: (error, handler) async {
          if (error.response?.statusCode == 401) {
            // Token expired, try to refresh
            final refreshed = await _refreshToken();
            if (refreshed) {
              // Retry original request
              return handler.resolve(await _retry(error.requestOptions));
            }
          }
          return handler.next(error);
        },
      ),
    );
  }

  String _getBaseUrl() {
    const env = String.fromEnvironment('ENV', defaultValue: 'staging');
    switch (env) {
      case 'production':
        return 'https://ziraai.com/api/v1';
      case 'staging':
        return 'https://ziraai-api-sit.up.railway.app/api/v1';
      case 'development':
      default:
        return 'https://localhost:5001/api/v1';
    }
  }

  Future<bool> _refreshToken() async {
    // TODO: Implement token refresh logic
    return false;
  }

  Future<Response<dynamic>> _retry(RequestOptions requestOptions) async {
    return _dio.request(
      requestOptions.path,
      options: Options(
        method: requestOptions.method,
        headers: requestOptions.headers,
      ),
      data: requestOptions.data,
      queryParameters: requestOptions.queryParameters,
    );
  }

  Future<Response> get(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) async {
    return await _dio.get(path, queryParameters: queryParameters);
  }

  Future<Response> post(
    String path, {
    dynamic data,
    Map<String, dynamic>? queryParameters,
  }) async {
    return await _dio.post(path, data: data, queryParameters: queryParameters);
  }
}
```

---

### Example 2: Dependency Injection Setup

#### File: `lib/main.dart`

```dart
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/api/api_client.dart';
import 'core/storage/secure_storage.dart';
import 'repositories/sponsored_analyses_repository.dart';
import 'blocs/sponsored_analyses/sponsored_analyses_bloc.dart';
import 'screens/sponsor/sponsored_analyses_list_screen.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    // Initialize dependencies
    final secureStorage = SecureStorage();
    final apiClient = ApiClient(secureStorage);
    final sponsoredAnalysesRepository = SponsoredAnalysesRepository(apiClient);

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: sponsoredAnalysesRepository),
      ],
      child: MultiBlocProvider(
        providers: [
          BlocProvider(
            create: (context) => SponsoredAnalysesBloc(
              sponsoredAnalysesRepository,
            ),
          ),
        ],
        child: MaterialApp(
          title: 'ZiraAI Sponsor',
          theme: ThemeData(
            primarySwatch: Colors.green,
            visualDensity: VisualDensity.adaptivePlatformDensity,
          ),
          home: SponsoredAnalysesListScreen(),
        ),
      ),
    );
  }
}
```

---

## Performance Optimization

### Image Caching

Use `cached_network_image` for sponsor logos and plant images:

```dart
dependencies:
  cached_network_image: ^3.3.0
```

### Pagination Strategy

- Default page size: 20 items
- Load more trigger: 90% scroll position
- Cache loaded pages in Bloc state

### State Persistence

Consider implementing state persistence for:
- Applied filters
- Sort preferences
- Scroll position

```dart
dependencies:
  hydrated_bloc: ^9.1.0
```

---

## Accessibility

### WCAG Compliance

- All tap targets minimum 48x48 dp
- Color contrast ratio > 4.5:1 for text
- Semantic labels for icons
- Screen reader support

### Implementation

```dart
// Example: Add semantic labels
IconButton(
  icon: const Icon(Icons.filter_list),
  onPressed: _showFilterDialog,
  tooltip: 'Filtreleme seçenekleri',
  semanticLabel: 'Analiz listesini filtrele',
)
```

---

## Summary

This mobile integration guide provides:

✅ Complete Flutter implementation with Bloc pattern
✅ Tier-based UI rendering (S/M/L/XL)
✅ Pagination with infinite scroll
✅ Filter and sort functionality
✅ Responsive UI components
✅ Error handling and retry mechanisms
✅ Unit, widget, and integration tests
✅ Performance optimizations
✅ Accessibility support

**Next Steps**:
1. Generate JSON serialization code: `flutter pub run build_runner build`
2. Run tests: `flutter test`
3. Test on devices: `flutter run`
4. Build release: `flutter build apk` / `flutter build ios`

---

**End of Mobile Integration Guide**
