# Tier Selection Mobile Wireframes

**Version:** 1.0
**Date:** 2025-10-12
**Platform:** Flutter (iOS/Android)
**Target:** Mobile Development Team

---

## 📱 Screen Flow

```
Purchase Flow Entry
       ↓
Tier Selection Screen ← [You are here]
       ↓
Quantity Selection Screen
       ↓
Payment Details Screen
       ↓
Purchase Confirmation
```

---

## 🎨 Screen 1: Tier Selection (Main View)

### Layout Overview

```
┌─────────────────────────────────────┐
│ ← [Back]    Paket Seçimi           │ ← AppBar
├─────────────────────────────────────┤
│                                     │
│  Size En Uygun Paketi Seçin       │ ← Header
│  Paketler, çiftçilere sağladığınız│
│  ayrıcalıkları belirler            │
│                                     │
├─────────────────────────────────────┤
│ [Horizontal Scroll: Tier Cards]    │ ← Main Content
│                                     │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐│
│ │  S   │ │  M   │ │  L   │ │  XL  ││
│ │      │ │[POP] │ │      │ │      ││
│ │ 50₺  │ │ 100₺ │ │ 200₺ │ │ 500₺ ││
│ └──────┘ └──────┘ └──────┘ └──────┘│
│   →      → →      →                 │
│                                     │
├─────────────────────────────────────┤
│ ▼ Detaylı Özellik Karşılaştırması │ ← Expandable Section
├─────────────────────────────────────┤
│                                     │
│        [Devam Et]                  │ ← Bottom Button
│                                     │
└─────────────────────────────────────┘
```

---

## 🃏 Tier Card Design

### S Tier Card (Unselected)

```
┌────────────────────────────────┐
│                                │ ← 280px width
│  Small - Basic Visibility      │ ← 20pt bold
│  ────────────────────────      │
│                                │
│  50₺                          │ ← 32pt bold, green
│  /ay                          │ ← 14pt gray
│                                │
│  ────────────────────────      │
│                                │
│  📊 Veri Erişimi       30%    │ ← 13pt
│  🖼️ Logo Görünürlüğü   1 ekran│
│                                │
│  ────────────────────────      │
│                                │
│  ✓ Başlangıç ekranında logo   │ ← Feature list
│  ✗ Mesajlaşma yok             │   13pt gray
│  ✗ Akıllı linkler yok         │
│                                │
└────────────────────────────────┘
   ↑ Gray border (1px)
```

### M Tier Card (Selected + Popular)

```
┌────────────────────────────────┐
│         [EN POPÜLER]           │ ← Orange badge
│  ═══════════════════════════   │   top-right
│                                │
│  Medium - Enhanced Visibility  │ ← 20pt bold
│  ────────────────────────      │
│                                │
│  100₺                         │ ← 32pt bold, green
│  /ay                          │
│                                │
│  ────────────────────────      │
│                                │
│  📊 Veri Erişimi       60%    │
│  🖼️ Logo Görünürlüğü   2 ekran│
│                                │
│  ────────────────────────      │
│                                │
│  ✓ Ürün türü ve hastalık bilgisi│
│  ✓ İlçe seviyesi konum        │
│  ✗ Mesajlaşma yok             │
│                                │
└────────────────────────────────┘
   ↑ Green border (3px) - Selected
   ↑ Light green background
```

### XL Tier Card (Unselected + Premium)

```
┌────────────────────────────────┐
│                                │
│  XL - Premium + Smart Links    │ ← 20pt bold
│  ────────────────────────      │
│                                │
│  500₺                         │ ← 32pt bold, green
│  /ay                          │
│                                │
│  ────────────────────────      │
│                                │
│  📊 Veri Erişimi      100%    │
│  🖼️ Logo Görünürlüğü   4 ekran│
│  💬 Mesajlaşma        Aktif    │
│  🔗 Akıllı Linkler    50 adet  │ ← Unique feature
│                                │
│  ────────────────────────      │
│                                │
│  ✓ Tam veri erişimi           │
│  ✓ Çiftçi ile mesajlaşma      │
│  ✓ Ürün tanıtım linkleri      │ ← Highlighted
│  ✓ Öncelikli destek (12 saat) │
│                                │
└────────────────────────────────┘
```

---

## 📊 Expandable Comparison Table

### Collapsed State

```
┌─────────────────────────────────────┐
│ ▼ Detaylı Özellik Karşılaştırması │
│   Tüm özellikleri yan yana görmek  │
│   için dokunun                      │
└─────────────────────────────────────┘
```

### Expanded State

```
┌─────────────────────────────────────┐
│ ▲ Detaylı Özellik Karşılaştırması │
├─────────────────────────────────────┤
│                                     │
│ [Horizontal Scroll Table]          │
│                                     │
│ Özellik        │ S  │ M  │ L  │ XL │
│ ───────────────┼────┼────┼────┼────│
│ Çiftçi İletişim│ ✗  │ ✗  │ ✓  │ ✓  │
│ Konum (İlçe)   │ ✗  │ ✓  │ ✓  │ ✓  │
│ Ürün Türleri   │ ✗  │ ✓  │ ✓  │ ✓  │
│ Tam Analiz     │ ✗  │ ✗  │ ✓  │ ✓  │
│ Mesajlaşma     │ ✗  │ ✗  │ ✓  │ ✓  │
│ Akıllı Linkler │ ✗  │ ✗  │ ✗  │ ✓  │
│                │    │    │    │    │
│      →         →    →    →         │
│                                     │
└─────────────────────────────────────┘
```

---

## 🎯 Bottom Button States

### No Selection

```
┌─────────────────────────────────────┐
│                                     │
│        [Devam Et]                  │ ← Disabled
│                                     │   Gray background
└─────────────────────────────────────┘
```

### Tier Selected

```
┌─────────────────────────────────────┐
│                                     │
│        [Devam Et]                  │ ← Enabled
│  M Tier seçildi - 100 TRY/ay      │   Green background
│                                     │   White text
└─────────────────────────────────────┘
```

---

## 🎨 Color Palette

```
Primary Green:       #4CAF50
Light Green:         #E8F5E9 (selected card bg)
Dark Green:          #2E7D32 (text)
Orange (popular):    #FF9800
Red (unavailable):   #F44336
Gray (border):       #E0E0E0
Dark Gray (text):    #424242
Light Gray (bg):     #F5F5F5
White:               #FFFFFF
```

---

## 📐 Spacing & Sizing

```
Card Width:          280px
Card Height:         Auto (min 280px)
Card Padding:        20px
Card Margin Right:   16px
Card Border Radius:  16px
Card Border Width:   1px (unselected), 3px (selected)

Button Height:       50px
Button Margin:       16px all sides
Button Border Radius: 8px

Text Sizes:
- Title: 20pt bold
- Price: 32pt bold
- Price Unit: 14pt regular
- Features: 13pt regular
- Feature Values: 13pt bold
- Badges: 12pt bold
```

---

## 🔄 Interactions

### 1. Card Selection

```
User taps card
       ↓
Card border changes: gray → green (3px)
       ↓
Card background changes: white → light green
       ↓
Other cards reset to unselected state
       ↓
Bottom button enables with tier name
       ↓
Haptic feedback (light impact)
```

### 2. Horizontal Scroll

```
User swipes left/right
       ↓
Cards scroll smoothly (snap to center optional)
       ↓
Scroll indicator shows position (4 cards)
```

### 3. Expandable Table

```
User taps "Detaylı Özellik Karşılaştırması"
       ↓
Arrow rotates: ▼ → ▲
       ↓
Table slides down with animation (300ms)
       ↓
User can scroll table horizontally
```

### 4. Continue Button

```
User taps "Devam Et" (when enabled)
       ↓
Button shows loading state (spinner)
       ↓
Navigate to Quantity Selection Screen
       ↓
Pass selected tier ID + tier DTO
```

---

## 📱 Screen States

### 1. Loading State

```
┌─────────────────────────────────────┐
│ ← [Back]    Paket Seçimi           │
├─────────────────────────────────────┤
│                                     │
│         [Loading Spinner]          │
│                                     │
│    Paketler yükleniyor...          │
│                                     │
└─────────────────────────────────────┘
```

### 2. Error State

```
┌─────────────────────────────────────┐
│ ← [Back]    Paket Seçimi           │
├─────────────────────────────────────┤
│                                     │
│           ⚠️                        │
│                                     │
│    Paketler yüklenemedi            │
│                                     │
│    [Tekrar Dene]                   │
│                                     │
└─────────────────────────────────────┘
```

### 3. Success State (Loaded)

```
┌─────────────────────────────────────┐
│ ← [Back]    Paket Seçimi           │
├─────────────────────────────────────┤
│  Size En Uygun Paketi Seçin       │
│                                     │
│ [4 tier cards loaded and displayed]│
│                                     │
│ [Expandable comparison available]  │
│                                     │
│        [Devam Et - disabled]       │
└─────────────────────────────────────┘
```

---

## 🎬 Animation Specs

### Card Selection Animation

```dart
// Flutter example
AnimatedContainer(
  duration: Duration(milliseconds: 300),
  curve: Curves.easeInOut,
  decoration: BoxDecoration(
    border: Border.all(
      color: isSelected ? Colors.green : Colors.grey[300]!,
      width: isSelected ? 3 : 1,
    ),
    borderRadius: BorderRadius.circular(16),
    color: isSelected ? Colors.green.shade50 : Colors.white,
  ),
  // ... card content
)
```

### Scroll Snap (Optional)

```dart
PageView.builder(
  controller: PageController(viewportFraction: 0.85),
  itemCount: tiers.length,
  itemBuilder: (context, index) {
    return TierCard(tier: tiers[index]);
  },
)
```

### Expansion Animation

```dart
ExpansionTile(
  title: Text('Detaylı Özellik Karşılaştırması'),
  children: [
    AnimatedSize(
      duration: Duration(milliseconds: 300),
      child: ComparisonTable(),
    ),
  ],
)
```

---

## 📋 Component Checklist

### TierCard Widget
- [ ] Card container with dynamic border/background
- [ ] Popular badge (conditional)
- [ ] Tier name and display name
- [ ] Price display (large, bold)
- [ ] Feature list (icons + text)
- [ ] Tap gesture handler
- [ ] Selected state management

### TierSelectionScreen Widget
- [ ] AppBar with back button
- [ ] Header section (title + subtitle)
- [ ] Horizontal scrollable tier cards
- [ ] Expandable comparison table
- [ ] Bottom button with selection state
- [ ] Loading state
- [ ] Error state with retry

### ComparisonTable Widget
- [ ] Horizontal scrollable table
- [ ] Feature rows with checkmarks
- [ ] Tier columns (S/M/L/XL)
- [ ] Responsive column widths

---

## 🧪 Test Cases

### Functional Tests
- [ ] Tiers load successfully from API
- [ ] Error handling when API fails
- [ ] Card selection updates state correctly
- [ ] Only one card can be selected at a time
- [ ] Bottom button enables/disables correctly
- [ ] Navigation passes correct tier data
- [ ] Comparison table expands/collapses
- [ ] Horizontal scroll works smoothly

### Visual Tests
- [ ] Popular badge appears on M tier
- [ ] Selected card has green border and background
- [ ] Unselected cards have gray border
- [ ] All tier features display correctly
- [ ] Prices formatted correctly (TRY)
- [ ] Icons display correctly
- [ ] Text is readable on all screen sizes

### Interaction Tests
- [ ] Haptic feedback on card selection
- [ ] Smooth animations (no jank)
- [ ] Scroll snap works correctly (if enabled)
- [ ] Button tap navigates to next screen
- [ ] Back button returns to previous screen

---

## 📱 Responsive Behavior

### Small Phones (< 375px width)
- Card width: 260px
- Font size: 90% of default
- Padding: 16px (reduced from 20px)

### Standard Phones (375-414px width)
- Card width: 280px (default)
- Font size: 100%
- Padding: 20px

### Large Phones (> 414px width)
- Card width: 300px
- Font size: 100%
- Padding: 24px

### Tablets (> 768px width)
- Show 2 cards side-by-side
- Card width: 45% of screen width
- Larger fonts and spacing

---

## 🔗 Related Files

**Flutter Models:**
- `lib/models/sponsorship_tier_comparison.dart`
- `lib/models/sponsorship_features.dart`

**Flutter Screens:**
- `lib/screens/sponsorship/tier_selection_screen.dart`

**Flutter Widgets:**
- `lib/widgets/sponsorship/tier_card.dart`
- `lib/widgets/sponsorship/tier_comparison_table.dart`

**Flutter Services:**
- `lib/services/sponsorship_service.dart`

---

## 📞 Support

**Questions on:**
- Design: Product Design Team
- Implementation: Mobile Development Team
- API: Backend Team

**References:**
- [UI Specification](./TIER_SELECTION_UI_SPECIFICATION.md)
- [Implementation Plan](./TIER_SELECTION_IMPLEMENTATION_PLAN.md)
- [Quick Reference](./TIER_COMPARISON_QUICK_REFERENCE.md)

---

**Status:** ✅ Ready for Implementation
**Est. Implementation Time:** 4-6 hours
**Last Updated:** 2025-10-12
