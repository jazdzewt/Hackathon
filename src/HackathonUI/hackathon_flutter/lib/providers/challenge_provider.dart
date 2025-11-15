import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../widgets/challenge_list_widget.dart';

// ten provider zarządza stanem wyzwań i paginacją
// korzysta z SharedPreferences do cachowania bieżącej strony i zapiswania jej stanu za każdym razem, gdy użytkownik zmienia stronę
class ChallengeProvider with ChangeNotifier {

  static const String _pageCacheKey = 'challengePage';

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

  final List<Challenge> _allChallenges = generateDummyChallenges(100);

  int _currentPage = 1; 
  final int _pageSize = 10;
  bool _stateLoaded = false; 


  int get totalPages => (_allChallenges.length / _pageSize).ceil();
  int get currentPage => _currentPage;

  List<Challenge> get challengesForCurrentPage {
    final startIndex = (_currentPage - 1) * _pageSize;
    return _allChallenges.skip(startIndex).take(_pageSize).toList();
  }



  void nextPage() {
    if (_currentPage < totalPages) {
      _currentPage++;
      notifyListeners();
      _saveCurrentPageToCache();
    }
  }

  void previousPage() {
    if (_currentPage > 1) {
      _currentPage--;
      notifyListeners();
      _saveCurrentPageToCache(); 
    }
  }

  // cachowanie danych o stronie
  /// Wczytuje ostatnio zapisany numer strony z pamięci przeglądarki
  Future<void> loadStateFromCache() async {
    // Zapobiegaj wielokrotnemu wczytywaniu (np. przy hot-reload)
    if (_stateLoaded) return;

    final prefs = await SharedPreferences.getInstance();
    _currentPage = prefs.getInt(_pageCacheKey) ?? 1;

    _stateLoaded = true;
    notifyListeners(); // Powiadom widgety, że wczytaliśmy stan
  }

  /// Zapisuje bieżący numer strony w pamięci przeglądarki
  Future<void> _saveCurrentPageToCache() async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setInt(_pageCacheKey, _currentPage);
  }
}