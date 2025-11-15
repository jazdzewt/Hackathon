import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart'; // Do formatowania dat
import 'package:provider/provider.dart';
import '../providers/challenge_provider.dart';

// WAŻNE: Popraw tę ścieżkę, jeśli jest inna!
import '../services/token_storage.dart';

// --- MODELE DANYCH ---

// Model ChallengeFull jest zgodny z Twoim JSON-em 'GET {id}' i 'PUT {id}'
class ChallengeFull {
  final String id;
  String title;
  String description;
  String evaluationMetric;
  DateTime submissionDeadline;
  bool isActive;
  String? datasetUrl;
  int maxFileSizeMb;
  List<String> allowedFileTypes;

  ChallengeFull({
    required this.id,
    required this.title,
    required this.description,
    required this.evaluationMetric,
    required this.submissionDeadline,
    required this.isActive,
    this.datasetUrl,
    required this.maxFileSizeMb,
    required this.allowedFileTypes,
  });

  factory ChallengeFull.fromJson(Map<String, dynamic> json) {
    return ChallengeFull(
      id: json['id'],
      title: json['title'],
      description: json['description'],
      evaluationMetric: json['evaluationMetric'],
      submissionDeadline: DateTime.parse(json['submissionDeadline']),
      isActive: json['isActive'],
      datasetUrl: json['datasetUrl'],
      maxFileSizeMb: json['maxFileSizeMb'],
      allowedFileTypes: List<String>.from(json['allowedFileTypes'] ?? []),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'description': description,
      'evaluationMetric': evaluationMetric,
      'submissionDeadline': submissionDeadline.toIso8601String(),
      'isActive': isActive,
      'datasetUrl': datasetUrl,
      'maxFileSizeMb': maxFileSizeMb,
      'allowedFileTypes': allowedFileTypes,
    };
  }
}

// --- ZAKTUALIZOWANY MODEL SUBMISSION ---
// Ten model pasuje teraz do Twojej nowej dokumentacji API
class Submission {
  final String id;
  final String challengeId;
  final String? fileName;
  double? score;
  final String? status;
  final DateTime submittedAt;

  Submission({
    required this.id,
    required this.challengeId,
    this.fileName,
    this.score,
    this.status,
    required this.submittedAt,
  });

  factory Submission.fromJson(Map<String, dynamic> json) {
    return Submission(
      id: json['id'] as String,
      challengeId: json['challengeId'] as String,
      fileName: json['fileName'],
      score: (json['score'] as num?)?.toDouble(),
      status: json['status'],
      submittedAt: DateTime.parse(json['submittedAt']),
    );
  }
}
// ------------------------------------

// --- GŁÓWNY WIDGET PANELU ADMINA ---
class ChallengeAdminPage extends StatefulWidget {
  final String challengeId;
  const ChallengeAdminPage({super.key, required this.challengeId});

  @override
  State<ChallengeAdminPage> createState() => _ChallengeAdminPageState();
}

class _ChallengeAdminPageState extends State<ChallengeAdminPage> {
  // Stan
  bool _isLoading = true;
  bool _isEditing = false;
  bool _isSaving = false;
  ChallengeFull? _challenge;
  String? _apiError;

  // Kontrolery formularza
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _titleController;
  late TextEditingController _descriptionController;
  late TextEditingController _metricController;
  late TextEditingController _datasetUrlController;
  DateTime _selectedDeadline = DateTime.now();

  // Stan tabeli zgłoszeń
  List<Submission> _submissions = [];
  bool _isLoadingSubmissions = false;
  int _submissionsPage = 1;
  bool _hasMoreSubmissions = true;
  final ScrollController _scrollController = ScrollController();

  // URL API (Dostosuj!)
  final String _apiBaseUrl = 'http://localhost:5043/api';

  @override
  void initState() {
    super.initState();
    // Inicjalizuj puste kontrolery
    _titleController = TextEditingController();
    _descriptionController = TextEditingController();
    _metricController = TextEditingController();
    _datasetUrlController = TextEditingController();

    _scrollController.addListener(() {
      if (_scrollController.position.pixels ==
          _scrollController.position.maxScrollExtent) {
        if (!_isLoadingSubmissions && _hasMoreSubmissions) {
          _fetchSubmissions(page: _submissionsPage + 1);
        }
      }
    });

    _fetchChallengeDetails();
    _fetchSubmissions(page: 1);
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _metricController.dispose();
    _datasetUrlController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  // --- LOGIKA API ---

  Future<String?> _getToken() => TokenStorage.getToken();

  /// Pobiera pełne szczegóły wyzwania (do formularza)
  Future<void> _fetchChallengeDetails() async {
    setState(() => _isLoading = true);
    final token = await _getToken();
    if (token == null) {
      setState(() {
        _apiError = 'Brak autoryzacji';
        _isLoading = false;
      });
      return;
    }

    try {
      // Zakładamy, że ten endpoint GET /api/Challenges/{id} jest poprawny
      final response = await http.get(
        Uri.parse('$_apiBaseUrl/Challenges/${widget.challengeId}'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200) {
        print("Dla fetch challenge details sie udalo: ${response.body}");
        final data = json.decode(response.body);
        _challenge = ChallengeFull.fromJson(data);
        // Ustaw kontrolery na dane z API
        _titleController.text = _challenge!.title;
        _descriptionController.text = _challenge!.description;
        _metricController.text = _challenge!.evaluationMetric;
        _datasetUrlController.text = _challenge!.datasetUrl ?? '';
        _selectedDeadline = _challenge!.submissionDeadline;
      } else {
        _apiError =
            'Błąd pobierania danych: ${response.statusCode} - ${response.body}';
      }
    } catch (e) {
      _apiError = 'Błąd sieci: $e';
    }
    setState(() => _isLoading = false);
  }

  /// Pobiera listę zgłoszeń (z paginacją)
  Future<void> _fetchSubmissions({required int page}) async {
    setState(() => _isLoadingSubmissions = true);
    final token = await _getToken();
    if (token == null) return;

    try {
      // Endpoint zgodny z Twoim CUrl: /Admin/submissions/challenges/{id}
      final response = await http.get(
        Uri.parse(
            '$_apiBaseUrl/Admin/submissions/challenges/${widget.challengeId}'),
        headers: {
          'Authorization': 'Bearer $token',
          // --- POPRAWKA: Prosimy o JSON, a nie text/plain ---
          'Accept': 'application/json'
        },
      );
      print(
          "To moj url: $_apiBaseUrl/Admin/submissions/challenges/${widget.challengeId}");
      if (response.statusCode == 200) {
        final List<dynamic> data = json.decode(response.body) ?? [];
        final newSubmissions =
            data.map((json) => Submission.fromJson(json)).toList();

        setState(() {
          _submissionsPage = page;
          if (page == 1) _submissions.clear();
          _submissions.addAll(newSubmissions);
          _hasMoreSubmissions = newSubmissions.length == 10;
        });
      } else {
        _showError('Błąd pobierania zgłoszeń: ${response.statusCode}');
      }
    } catch (e) {
      _showError('Błąd sieci (zgłoszenia): $e');
      print('Błąd sieci (zgłoszenia): $e');
    }
    setState(() => _isLoadingSubmissions = false);
  }

  /// Zapisuje zmiany w wyzwaniu (PUT)
  Future<void> _saveChallenge({bool exit = false}) async {
  // 1. Walidacja formularza (bez zmian)
  if (!_formKey.currentState!.validate()) return;

  setState(() => _isSaving = true);
  final token = await _getToken();
  if (token == null) {
    setState(() => _isSaving = false); // Upewnij się, że odblokujesz UI
    return;
  }


  final Map<String, dynamic> jsonBody = {
    // Mapujemy pola z formularza na pola oczekiwane przez API
    "name": _titleController.text,
    "fullDescription": _descriptionController.text,
    "evaluationMetric": _metricController.text,
    "endDate": _selectedDeadline.toIso8601String(), // Poprawny format daty
    "isActive": _challenge?.isActive ?? true, // Użyj istniejącej wartości


    "shortDescription": _descriptionController.text.length > 100
        ? _descriptionController.text.substring(0, 100)
        : _descriptionController.text, // Tymczasowe obejście
    "rules": "TODO: Dodaj pole 'rules' do formularza", // Tymczasowe
    "startDate": DateTime.now().toIso8601String() // Tymczasowe
  };

  try {
    final response = await http.put(
      Uri.parse('$_apiBaseUrl/Admin/challenges/${widget.challengeId}'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },

      body: json.encode(jsonBody),
    );

    if (response.statusCode == 200 || response.statusCode == 204) {
      setState(() => _isEditing = false);
      if (exit && context.mounted) {
        context.read<ChallengeProvider>().forceRefreshChallenges();
        context.go('/dashboard');
      }
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text('Zapisano pomyślnie!'),
            backgroundColor: Colors.green),
      );
    } else {
      // Błąd 400 (Bad Request) lub inny
      _showError('Błąd zapisu: ${response.statusCode} - ${response.body}');
    }
  } catch (e) {
    _showError('Błąd sieci: $e');
  }
  setState(() => _isSaving = false);
}

  /// Usuwa wyzwanie (DELETE)
  Future<void> _deleteChallenge() async {
    final bool? confirmed = await showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Potwierdź usunięcie'),
        content: Text(
            'Czy na pewno chcesz trwale usunąć wyzwanie "${_challenge?.title}"?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(ctx).pop(false),
              child: const Text('Anuluj')),
          TextButton(
              onPressed: () => Navigator.of(ctx).pop(true),
              child:
                  const Text('Usuń', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;

    setState(() => _isSaving = true);
    final token = await _getToken();
    if (token == null) return;

    try {
      // Endpoint zgodny z Twoją nową dokumentacją
      final response = await http.delete(
        Uri.parse('$_apiBaseUrl/Admin/challenges/${widget.challengeId}'),
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 200 || response.statusCode == 204) {
        if (context.mounted) {
          context.go('/dashboard');
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
                content: Text('Wyzwanie usunięte.'),
                backgroundColor: Colors.green),
          );
        }
      } else {
        _showError('Błąd usuwania: ${response.statusCode} - ${response.body}');
        print('Blad usuwania:  ${response.statusCode} - ${response.body} ');
      }
    } catch (e) {
      _showError('Błąd sieci: $e');
    }
    setState(() => _isSaving = false);
  }

  /// Zapisuje nową ocenę dla zgłoszenia
  // --- POPRAWKA: ID jest teraz Stringiem ---
  Future<void> _gradeSubmission(String submissionId, String score) async {
    final token = await _getToken();
    if (token == null) return;

    final double? scoreValue = double.tryParse(score);
    if (scoreValue == null) {
      _showError('Ocena musi być liczbą');
      return;
    }

    // TODO: Sprawdź, czy API /api/Admin/submissions/{id}/grade istnieje
    final url = '$_apiBaseUrl/Admin/submissions/$submissionId/score';

    try {
      final response = await http.post(
        Uri.parse(url),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: json.encode({'score': scoreValue}),
      );

      if (response.statusCode == 200) {
        _fetchSubmissions(page: 1); // Odśwież listę, aby pokazać nową ocenę
      } else {
        _showError('Błąd zapisu oceny: ${response.statusCode} - ${response.body}');
      }
    } catch (e) {
      _showError('Błąd sieci: $e');
    }
  }

  void _showError(String message) {
    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  // --- METODY BUDOWANIA UI ---

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Panel Admina: Wyzwanie #${widget.challengeId}'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: TextButton.icon(
              onPressed: () async {
                await TokenStorage.deleteToken();
                if (context.mounted) {
                  context.go('/');
                }
              },
              icon: const Icon(Icons.logout, color: Colors.white),
              label: const Text(
                'Wyloguj się',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: _buildMainContent(),
      ),
    );
  }

  Widget _buildMainContent() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_apiError != null) {
      return Center(
          child: Text(_apiError!, style: const TextStyle(color: Colors.red)));
    }
    if (_challenge == null) {
      return const Center(child: Text('Nie udało się wczytać wyzwania.'));
    }

    return AbsorbPointer(
      absorbing: _isSaving,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildHeaderButtons(),
          const Divider(height: 24, thickness: 1),
          Text('Edycja Wyzwania',
              style: Theme.of(context).textTheme.headlineMedium),
          const SizedBox(height: 16),
          _buildChallengeForm(),
          const Divider(height: 24, thickness: 1),
          Text('Zgłoszenia Użytkowników',
              style: Theme.of(context).textTheme.headlineMedium),
          const SizedBox(height: 16),
          _buildSubmissionsTable(),
        ],
      ),
    );
  }

  Widget _buildHeaderButtons() {
    // ... (Ta funkcja jest poprawna, bez zmian) ...
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.grey.shade100,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Wrap(
        // Użyj Wrap dla responsywności
        spacing: 12,
        runSpacing: 12,
        alignment: WrapAlignment.center,
        children: [
          // Przycisk EDYTUJ / ANULUJ
          ElevatedButton.icon(
            icon: Icon(_isEditing ? Icons.cancel : Icons.edit),
            label: Text(_isEditing ? 'Anuluj' : 'Edytuj'),
            style: ElevatedButton.styleFrom(
                backgroundColor: _isEditing
                    ? Colors.grey
                    : Theme.of(context).colorScheme.primary,
                foregroundColor: Colors.white),
            onPressed: () => setState(() => _isEditing = !_isEditing),
          ),
          ElevatedButton.icon(
            icon: const Icon(Icons.save_as),
            label: const Text('Zapisz i Wyjdź'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Theme.of(context).colorScheme.primary,
                foregroundColor: Colors.white),
            onPressed: _isEditing ? () => _saveChallenge(exit: true) : null,
          ),
          // Przycisk ZAPISZ I WYJDŹ
          ElevatedButton.icon(
            icon: const Icon(Icons.arrow_back),
            label: const Text('Wróć'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Theme.of(context).colorScheme.primary,
                foregroundColor: Colors.white),
            onPressed: () {
            context.go('/dashboard');
          },
          ),
          // Przycisk USUŃ
          ElevatedButton.icon(
            icon: const Icon(Icons.delete_forever),
            label: const Text('Usuń Wyzwanie'),
            style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red, foregroundColor: Colors.white),
            onPressed: _deleteChallenge,
          ),
        ],
      ),
    );
  }

  Widget _buildChallengeForm() {
    // ... (Ta funkcja jest poprawna, bez zmian) ...
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          TextFormField(
            controller: _titleController,
            readOnly: !_isEditing,
            decoration: const InputDecoration(
                labelText: 'Tytuł Wyzwania', border: OutlineInputBorder()),
            validator: (value) =>
                (value == null || value.isEmpty) ? 'Tytuł jest wymagany' : null,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _descriptionController,
            readOnly: !_isEditing,
            decoration: const InputDecoration(
                labelText: 'Opis', border: OutlineInputBorder()),
            maxLines: 5,
          ),
          const SizedBox(height: 16),
          // Responsywny layout dla metryki i deadline'u
          Wrap(
            spacing: 16,
            runSpacing: 16,
            children: [
              ConstrainedBox(
                constraints: const BoxConstraints(minWidth: 250),
                child: TextFormField(
                  controller: _metricController,
                  readOnly: !_isEditing,
                  decoration: const InputDecoration(
                      labelText: 'Metryka Oceny (np. accuracy)',
                      border: OutlineInputBorder()),
                ),
              ),
              // Wybór daty
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                decoration: BoxDecoration(
                    border: Border.all(color: Colors.grey.shade400),
                    borderRadius: BorderRadius.circular(4)),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Deadline:', style: TextStyle(fontSize: 12)),
                    TextButton.icon(
                      icon: const Icon(Icons.calendar_today),
                      label: Text(
                          DateFormat('yyyy-MM-dd HH:mm').format(_selectedDeadline)),
                      onPressed: !_isEditing
                          ? null
                          : () async {
                              final newDate = await showDatePicker(
                                context: context,
                                initialDate: _selectedDeadline,
                                firstDate: DateTime.now(),
                                lastDate:
                                    DateTime.now().add(const Duration(days: 365)),
                              );
                              if (newDate != null) {
                                // Pozwól na wybór godziny
                                final newTime = await showTimePicker(
                                    context: context,
                                    initialTime:
                                        TimeOfDay.fromDateTime(_selectedDeadline));
                                if (newTime != null) {
                                  setState(() {
                                    _selectedDeadline = DateTime(
                                        newDate.year,
                                        newDate.month,
                                        newDate.day,
                                        newTime.hour,
                                        newTime.minute);
                                  });
                                }
                              }
                            },
                    ),
                  ],
                ),
              )
            ],
          ),
        ],
      ),
    );
  }

  /// Buduje tabelę zgłoszeń (ZAKTUALIZOWANA)
  Widget _buildSubmissionsTable() {
    if (_isLoadingSubmissions && _submissions.isEmpty) {
      return const Center(
          child: Padding(
              padding: EdgeInsets.all(16.0), child: CircularProgressIndicator()));
    }
    if (_submissions.isEmpty) {
      return const Center(
          child: Padding(
              padding: EdgeInsets.all(16.0),
              child: Text('Brak zgłoszeń do wyświetlenia.')));
    }

    return Column(
      children: [
        LayoutBuilder(builder: (context, constraints) {
          if (constraints.maxWidth > 700) {
            // Zwiększyłem próg dla lepszej tabeli
            return _buildDataTable();
          } else {
            return _buildDataList();
          }
        }),
      ],
    );
  }

  /// Wersja tabeli na duże ekrany (ZAKTUALIZOWANA)
  Widget _buildDataTable() {
    return Column(
      children: [
        DataTable(
          columns: const [
            DataColumn(label: Text('Plik')), // Zamiast Info
            DataColumn(label: Text('Data')),
            DataColumn(label: Text('Status')),
            DataColumn(label: Text('Ocena')),
            DataColumn(label: Text('Akcje')),
          ],
          rows: _submissions.map((sub) {
            return DataRow(cells: [
              // --- ZMIANA: Pokaż fileName ---
              DataCell(Text(sub.fileName ?? 'ID: ${sub.id}')),
              DataCell(Text(DateFormat('MM-dd HH:mm').format(sub.submittedAt))),
              DataCell(
                Tooltip(
                  // API nie zwraca 'errorMessage', więc pokazuj status
                  message: sub.status ?? 'Brak statusu',
                  child: Text(sub.status ?? '',
                      style: TextStyle(
                          color: sub.status == 'Error'
                              ? Colors.red
                              : Colors.green)),
                ),
              ),
              DataCell(
                SizedBox(
                  width: 100,
                  child: TextFormField(
                    initialValue: sub.score?.toString() ?? '',
                    decoration: const InputDecoration(labelText: 'Ocena'),
                    keyboardType: TextInputType.number,
                    onFieldSubmitted: (value) =>
                        _gradeSubmission(sub.id, value),
                  ),
                ),
              ),
              DataCell(
                IconButton(
                  icon: const Icon(Icons.download),
                  tooltip: 'Pobierz plik',
                  // --- ZMIANA: Logika pobierania ---
                  // TODO: Backend musi wysłać URL do pobrania, nie tylko nazwę pliku
                  // Na razie przycisk jest wyłączony
                  onPressed: null,
                  // onPressed: sub.fileName == null ? null : () {
                  //   print('Pobieranie: ${sub.fileName}');
                  //   // TODO: Potrzebujesz endpointu GET /api/Admin/submissions/download/{submissionId}
                  // },
                ),
              ),
            ]);
          }).toList(),
        ),
        if (_hasMoreSubmissions)
          _isLoadingSubmissions
              ? const Padding(
                  padding: EdgeInsets.all(16), child: CircularProgressIndicator())
              : TextButton(
                  onPressed: () => _fetchSubmissions(page: _submissionsPage + 1),
                  child: const Text('Wczytaj więcej...'),
                )
      ],
    );
  }

  /// Wersja listy na małe ekrany (mobilne) (ZAKTUALIZOWANA)
  Widget _buildDataList() {
    return SizedBox(
      height: 400, // Ogranicz wysokość
      child: ListView.builder(
        controller: _scrollController,
        itemCount: _submissions.length + (_hasMoreSubmissions ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _submissions.length) {
            return _isLoadingSubmissions
                ? const Center(
                    child: Padding(
                        padding: EdgeInsets.all(16),
                        child: CircularProgressIndicator()))
                : const Center(
                    child: Padding(
                        padding: EdgeInsets.all(8.0),
                        child: Text('Koniec listy')));
          }

          final submission = _submissions[index];
          return Card(
            margin: const EdgeInsets.symmetric(vertical: 4),
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // --- ZMIANA: Pokaż fileName ---
                  Text('Plik: ${submission.fileName ?? 'ID: ${submission.id}'}',
                      style: const TextStyle(fontWeight: FontWeight.bold)),
                  Text('Zgłoszono: ${DateFormat('yyyy-MM-dd HH:mm').format(submission.submittedAt)}'),
                  Text('Status: ${submission.status ?? 'Brak'}'),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: TextFormField(
                          initialValue: submission.score?.toString() ?? '',
                          decoration: const InputDecoration(
                              labelText: 'Ocena', border: OutlineInputBorder()),
                          keyboardType: TextInputType.number,
                          onFieldSubmitted: (value) =>
                              _gradeSubmission(submission.id, value),
                        ),
                      ),
                      IconButton(
                        icon: const Icon(Icons.download),
                        tooltip: 'Pobierz plik',
                        // --- ZMIANA: Logika pobierania ---
                        onPressed: null, // Na razie wyłączone
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}