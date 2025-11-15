import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../widgets/challenge_list_widget.dart';
import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import '../services/token_storage.dart';
import 'dart:html' as html; 

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
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
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

  Future<void> forceRefreshChallenges() async {
      _challengesLoaded = false; // To jest klucz! Resetujemy flagę cache'u.
      _allChallenges.clear(); // Czyścimy starą listę.
      _currentPage = 1; // Wracamy na pierwszą stronę.
      _stateLoaded = false; // Wymuś ponowne wczytanie stanu (strony)

      // Opcjonalnie: wyczyść zapamiętaną stronę z cache'u
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_pageCacheKey);

      print('[ChallengeProvider] Pamięć cache unieważniona. Gotowy do odświeżenia.');
      // Nie trzeba wołać notifyListeners() ani loadChallengesFromApi() tutaj.
      // Zrobi to 'loadStateFromCache()' na DashboardPage.
    }

  // cachowanie danych o stronie
  /// Wczytuje ostatnio zapisany numer strony z pamięci przeglądarki
  Future<void> loadStateFromCache() async {
    // Zapobiegaj wielokrotnemu wczytywaniu (np. przy hot-reload)
    if (_stateLoaded) return;

    _stateLoaded = true;
    notifyListeners(); // Powiadom od razu, że zaczynamy

    final prefs = await SharedPreferences.getInstance();
    _currentPage = prefs.getInt(_pageCacheKey) ?? 1;

    await loadChallengesFromApi();
    notifyListeners(); // Powiadom widgety, że wczytaliśmy stan
  }

  /// Zapisuje bieżący numer strony w pamięci przeglądarki
  Future<void> _saveCurrentPageToCache() async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setInt(_pageCacheKey, _currentPage);
  }

  /// Pobiera dane użytkownika z /api/Me
  Future<Map<String, dynamic>?> fetchCurrentUser() async {
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu dla /api/Me');
      return null;
    }

    final url = Uri.parse('http://localhost:5043/api/Me');
    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      print('Odpowiedź /api/Me: ${response.body}');
      print('Status code: ${response.statusCode}');
      
      if (response.statusCode == 200) {
        return json.decode(response.body) as Map<String, dynamic>;
      } else {
        print('Błąd: status ${response.statusCode}');
        return null;
      }
    } catch (e) {
      print('Błąd zapytania /api/Me: $e');
      return null;
    }
  }

  /// Pobiera szczegóły wyzwania z /api/Challenges/{id}
  Future<Map<String, dynamic>?> fetchChallengeById(String challengeId) async {
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu dla /api/Challenges/$challengeId');
      return null;
    }

    final url = Uri.parse('http://localhost:5043/api/Challenges/$challengeId');
    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      print('Odpowiedź /api/Challenges/$challengeId: ${response.body}');
      print('Status code: ${response.statusCode}');
      
      if (response.statusCode == 200) {
        return json.decode(response.body) as Map<String, dynamic>;
      } else {
        print('Błąd: status ${response.statusCode}');
        return null;
      }
    } catch (e) {
      print('Błąd zapytania /api/Challenges/$challengeId: $e');
      return null;
    }
  }

  /// Pobiera nazwę użytkownika na podstawie userId
  /// Używa tokenu tego użytkownika do autoryzacji
  Future<String?> fetchUserNameById(String userId) async {
    // Generujemy token dla tego użytkownika (w rzeczywistości to nie zadziała tak prosto)
    // Ale zgodnie z wymaganiem, użyjemy /api/Me z tokenem w headerze
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu dla /api/Me');
      return null;
    }

    final url = Uri.parse('http://localhost:5043/api/Me');
    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      
      if (response.statusCode == 200) {
        final data = json.decode(response.body) as Map<String, dynamic>;
        // Sprawdź czy to właściwy użytkownik
        if (data['id'] == userId || data['userId'] == userId) {
          return data['displayName'] ?? data['email'] ?? data['username'] ?? data['name'];
        }
        return null;
      } else {
        print('Błąd: status ${response.statusCode}');
        return null;
      }
    } catch (e) {
      print('Błąd zapytania /api/Me: $e');
      return null;
    }
  }

  /// Pobiera leaderboard dla danego wyzwania
  Future<List<Map<String, dynamic>>?> fetchLeaderboard(String challengeId) async {
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu dla /api/Leaderboard/$challengeId');
      return null;
    }

    final url = Uri.parse('http://localhost:5043/api/Leaderboard/$challengeId');
    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      print('Odpowiedź /api/Leaderboard/$challengeId: ${response.body}');
      print('Status code: ${response.statusCode}');
      
      if (response.statusCode == 200) {
        final List<dynamic> data = json.decode(response.body);
        return data.cast<Map<String, dynamic>>();
      } else {
        print('Błąd: status ${response.statusCode}');
        return null;
      }
    } catch (e) {
      print('Błąd zapytania /api/Leaderboard/$challengeId: $e');
      return null;
    }
  }

  /// Przesyła rozwiązanie (plik) do wyzwania
  /// Zwraca null jeśli sukces, lub String z komunikatem błędu
  Future<String?> submitSolution(String challengeId, html.File file) async {
    final token = await TokenStorage.getToken();
    if (token == null) {
      return 'Brak tokenu autoryzacji';
    }

    print('=== SUBMIT SOLUTION DEBUG ===');
    print('Challenge ID: $challengeId');
    print('File name: ${file.name}');
    print('Token: ${token.substring(0, 20)}...');
    
    final url = 'http://localhost:5043/api/Submissions/challenges/$challengeId/submit';
    print('URL: $url');
    
    try {
      final formData = html.FormData();
      formData.appendBlob('File', file, file.name);  // Spróbuj z wielką literą
      
      print('FormData created with field: File');

      final xhr = html.HttpRequest();
      xhr.open('POST', url);
      xhr.setRequestHeader('Authorization', 'Bearer $token');
      
      print('XHR request prepared, sending...');
      
      final completer = Completer<String?>();
      
      xhr.onLoad.listen((event) {
        print('XHR Status: ${xhr.status}');
        print('XHR Response: ${xhr.responseText}');
        
        if (xhr.status == 200 || xhr.status == 201) {
          completer.complete(null);
        } else {
          // Zwróć cały JSON jako błąd
          final responseText = xhr.responseText ?? 'Brak odpowiedzi';
          String errorMessage = responseText.isEmpty ? 'Błąd ${xhr.status}' : responseText;
          
          // Spróbuj wyciągnąć pole 'error' z JSON
          try {
            final data = json.decode(responseText);
            if (data is Map && data.containsKey('error')) {
              errorMessage = data['error'].toString();
              print('Extracted error field: $errorMessage');
            }
          } catch (e) {
            print('Could not parse error JSON: $e');
          }
          
          completer.complete(errorMessage);
        }
      });

      xhr.onError.listen((event) {
        print('XHR Error: $event');
        completer.complete('Błąd połączenia');
      });

      xhr.send(formData);
      return await completer.future;
      
    } catch (e) {
      print('Exception: $e');
      return 'Wyjątek: $e';
    }
  }
}