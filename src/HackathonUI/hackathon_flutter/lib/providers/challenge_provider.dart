import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../widgets/challenge_list_widget.dart';
import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import '../services/token_storage.dart';
import 'dart:html' as html;

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
  String _searchQuery = '';

  int _currentPage = 1;
  final int _pageSize = 10;
  bool _stateLoaded = false; 
  bool _isLoading = false;
  bool _challengesLoaded = false;

  List<Challenge> get _filteredChallenges {
    if (_searchQuery.isEmpty) {
      return _allChallenges;
    }
    return _allChallenges.where((challenge) {
      return challenge.title.toLowerCase().contains(_searchQuery.toLowerCase()) ||
             challenge.description.toLowerCase().contains(_searchQuery.toLowerCase());
    }).toList();
  }

  int get totalPages => (_filteredChallenges.length / _pageSize).ceil();
  int get currentPage => _currentPage;
  
  bool get isLoading => _isLoading;
  
  List<Challenge> get challengesForCurrentPage {
    final startIndex = (_currentPage - 1) * _pageSize;
    return _filteredChallenges.skip(startIndex).take(_pageSize).toList();
  }

  void setSearchQuery(String query) {
    _searchQuery = query;
    _currentPage = 1;
    notifyListeners();
  }

  Future<void> loadChallengesFromApi() async {
    if (_isLoading || _challengesLoaded) return;
    
    final token = await TokenStorage.getToken();
    if (token == null) {
      print('Brak tokenu, logowanie jest wymagane.');
      return;
    }

    _isLoading = true;
    notifyListeners();

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

        _challengesLoaded = true;

      } else {
        print('Błąd podczas pobierania wyzwań: ${response.statusCode}');
        print('Odpowiedź serwera: ${response.body}');
      }
    } catch (e) {
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
      _challengesLoaded = false;
      _allChallenges.clear();
      _currentPage = 1;
      _stateLoaded = false;

      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_pageCacheKey);

      print('[ChallengeProvider] Pamięć cache unieważniona. Gotowy do odświeżenia.');
    }

  Future<void> loadStateFromCache() async {
    if (_stateLoaded) return;

    _stateLoaded = true;
    notifyListeners();

    final prefs = await SharedPreferences.getInstance();
    _currentPage = prefs.getInt(_pageCacheKey) ?? 1;

    await loadChallengesFromApi();
    notifyListeners();
  }

  Future<void> _saveCurrentPageToCache() async {
    final prefs = await SharedPreferences.getInstance();
    prefs.setInt(_pageCacheKey, _currentPage);
  }

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

  Future<String?> fetchUserNameById(String userId) async {
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
      formData.appendBlob('File', file, file.name);
      
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
          final responseText = xhr.responseText ?? 'Brak odpowiedzi';
          String errorMessage = responseText.isEmpty ? 'Błąd ${xhr.status}' : responseText;
          
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