import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../widgets/challenge_list_widget.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../services/token_storage.dart'; 

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

  final List<Challenge> _allChallenges = [];

  int _currentPage = 1; 
  final int _pageSize = 10;
  bool _stateLoaded = false; 
  bool _isLoading = false;
  bool _challengesLoaded = false;

  int get totalPages => (_allChallenges.length / _pageSize).ceil();
  int get currentPage => _currentPage;
  
  bool get isLoading => _isLoading;
  
  List<Challenge> get challengesForCurrentPage {
    final startIndex = (_currentPage - 1) * _pageSize;
    return _allChallenges.skip(startIndex).take(_pageSize).toList();
  }

  Future<void> loadChallengesFromApi() async {
    // Sprawdź, czy już nie ładujemy LUB czy już pomyślnie nie załadowaliśmy
    if (_isLoading || _challengesLoaded) return;
    
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu, logowanie jest wymagane.');
      return;
    }

    _isLoading = true;
    notifyListeners();


    // Sprawdź port (5043?) i wielkość liter (Challenges?)
    final url = Uri.parse('http://localhost:5043/api/Challenges');

    try {
      final response = await http.get(
        url,
        
        headers: {
          'Content-Type': 'application/json'
        },
      );

      if (response.statusCode == 200) {
        final List<dynamic> data = json.decode(response.body);
        final List<Challenge> loadedChallenges =
            data.map((item) => Challenge.fromJson(item)).toList();
        
        print('Pobrano ${loadedChallenges.length} wyzwań z API.');

        _allChallenges.clear(); 
        _allChallenges.addAll(loadedChallenges);

        // --- POPRAWKA #4: USTAW FLAGĘ SUKCESU TYLKO TUTAJ ---
        _challengesLoaded = true;

      } else {
        // Błąd z serwera (np. 401 Unauthorized, 404 Not Found)
        print('Błąd podczas pobierania wyzwań: ${response.statusCode}');
        print('Odpowiedź serwera: ${response.body}');
      }
    } catch (e) {
      // Błąd sieci (np. Connection Refused, jeśli serwer nie działa)
      print('Wyjątek (błąd sieci) podczas pobierania wyzwań: $e');
    }
    
    _isLoading = false; 
    notifyListeners(); 
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
    await loadChallengesFromApi();
    notifyListeners(); // Powiadom widgety, że wczytaliśmy stan
  }

  /// Zapisuje bieżący numer strony w pamięci przeglądarki
  Future<void> _saveCurrentPageToCache() async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setInt(_pageCacheKey, _currentPage);
  }
}