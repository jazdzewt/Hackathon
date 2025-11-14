// lib/providers/challenge_provider.dart
import 'package:flutter/foundation.dart';
// Te 3 importy nie są już potrzebne do logiki z danymi testowymi
// import 'package:http/http.dart' as http;
// import 'dart:convert';
import '../widgets/challenge_list_widget.dart'; // Tu jest definicja Challenge

class ChallengeProvider with ChangeNotifier {
  static List<Challenge> generateDummyChallenges(int count) {
    return List<Challenge>.generate(
      count,
      (index) => Challenge(
        id: 'id_$index',
        title: 'Wyzwanie #$index',
        description: 'Opis wyzwania numer $index',
      ),
    );
  }

  // --- STAN PROVIDERA ---
  
  /// Pełna lista wszystkich 100 wyzwań (nasza "baza danych")
  final List<Challenge> _allChallenges = generateDummyChallenges(100);
  
  int _currentPage = 1;
  final int _pageSize = 10; // Chcemy po 10 na stronie

  // --- GETTERY DLA WIDGETU ---

  /// Zwraca łączną liczbę stron (100 / 10 = 10)
  int get totalPages => (_allChallenges.length / _pageSize).ceil();

  /// Zwraca obecny numer strony (do wyświetlenia "Strona 1 z 10")
  int get currentPage => _currentPage;

  /// !! KLUCZOWA ZMIANA !!
  /// Ten getter oblicza i zwraca tylko listę 10 wyzwań dla bieżącej strony.
  List<Challenge> get challengesForCurrentPage {
    final startIndex = (_currentPage - 1) * _pageSize;
    
    // .skip() pomija pierwsze X elementów, .take() pobiera następne Y
    return _allChallenges.skip(startIndex).take(_pageSize).toList();
  }

  // --- AKCJE (METODY) DLA WIDGETU ---

  /// Przechodzi do następnej strony, jeśli to możliwe
  void nextPage() {
    if (_currentPage < totalPages) {
      _currentPage++;
      notifyListeners(); // Powiadom widgety, że strona się zmieniła
    }
  }

  /// Wraca do poprzedniej strony, jeśli to możliwe
  void previousPage() {
    if (_currentPage > 1) {
      _currentPage--;
      notifyListeners(); // Powiadom widgety, że strona się zmieniła
    }
  }


}