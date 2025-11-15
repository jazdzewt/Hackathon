import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import 'package:provider/provider.dart';
import '../services/token_storage.dart';
import '../providers/challenge_provider.dart';
import 'dart:html' as html;

class ChallengeUserPage extends StatefulWidget {
  // 1. Odbieramy 'challengeId' przekazane z routera
  final String challengeId;
  
  const ChallengeUserPage({
    super.key,
    required this.challengeId, // Jest to wymagane
  });

  @override
  State<ChallengeUserPage> createState() => _ChallengeUserPageState();
}

// 2. POPRAWIONA DEKLARACJA STANU
class _ChallengeUserPageState extends State<ChallengeUserPage> {
  Map<String, dynamic>? _challengeData;
  bool _isLoading = true;
  String? _selectedFileName;
  html.File? _selectedFile;

  @override
  void initState() {
    super.initState();
    _loadChallengeDetails();
  }

  Future<void> _loadChallengeDetails() async {
    final data = await context.read<ChallengeProvider>().fetchChallengeById(widget.challengeId);
    if (mounted) {
      setState(() {
        _challengeData = data;
        _isLoading = false;
      });
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        // 3. Używamy 'widget.challengeId', aby pokazać ID w tytule
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            context.go('/dashboard');
          },
        ),
        title: Text('Wyzwanie #${widget.challengeId}'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16.0),
            child: TextButton.icon(
              onPressed: () async {
                await TokenStorage.deleteToken();
                if (context.mounted) {
                  // Wracamy do strony logowania/głównej
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
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }
    
    if (_challengeData == null) {
      return const Center(
        child: Text('Nie udało się załadować szczegółów wyzwania'),
      );
    }
    String title = _challengeData?['title'] ?? 'Brak tytułu';
    String description = _challengeData?['description'] ?? 'Brak opisu';
    String data = _challengeData?['submissionDeadline'] ?? 'Brak danych';
    String isActive = _challengeData?['isActive'] == true ? 'Aktywne' : 'Nieaktywne';
    List<dynamic> allowedFileTypes = _challengeData?['allowedFileTypes'] ?? ['Dwolne'];
    return Center(
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 800),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              const SizedBox(height: 16),
              Column(
                crossAxisAlignment: CrossAxisAlignment.center,
                children: [
                  Text(
                    title,
                    style: Theme.of(context).textTheme.headlineLarge,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    isActive,
                    style: const TextStyle(
                      fontSize: 19,
                      color: Colors.grey,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
              const SizedBox(height: 24),
              Text(
                description,
                style: Theme.of(context).textTheme.headlineMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 16),
              Text(
                'Deadline: ${data.toString().replaceAll('T', ' ').substring(0, 16)}',
                style: Theme.of(context).textTheme.headlineSmall,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 32),

              SizedBox(
                width: 300,
                child: ElevatedButton.icon(
                  style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14.0),
                    backgroundColor: AppColors.primary,
                  ),
                  onPressed: () async {
                    // Jeśli plik już wybrany - prześlij go
                    if (_selectedFile != null) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('Przesyłanie pliku...'),
                          duration: Duration(seconds: 1),
                        ),
                      );
                      
                      final error = await context.read<ChallengeProvider>().submitSolution(
                        widget.challengeId,
                        _selectedFile!,
                      );
                      
                      if (context.mounted) {
                        if (error == null) {
                          // Sukces
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Plik przesłany pomyślnie!'),
                              backgroundColor: Colors.green,
                            ),
                          );
                          setState(() {
                            _selectedFile = null;
                            _selectedFileName = null;
                          });
                        } else {
                          // Błąd - pokaż komunikat i zresetuj wybór
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(
                              content: Text(error),
                              backgroundColor: Colors.red,
                              duration: const Duration(seconds: 4),
                            ),
                          );
                          setState(() {
                            _selectedFile = null;
                            _selectedFileName = null;
                          });
                        }
                      }
                      return;
                    }
                    
                    // Jeśli brak pliku - otwórz dialog wyboru
                    final html.FileUploadInputElement uploadInput = html.FileUploadInputElement();
                    uploadInput.click();
                    
                    uploadInput.onChange.listen((e) {
                      final files = uploadInput.files;
                      if (files != null && files.isNotEmpty) {
                        final file = files[0];
                        setState(() {
                          _selectedFile = file;
                          _selectedFileName = file.name;
                        });
                        
                        print('Wybrano plik: ${file.name}');
                        
                        ScaffoldMessenger.of(context).showSnackBar(
                          SnackBar(
                            content: Text('Wybrano plik: ${file.name}'),
                            backgroundColor: Colors.green,
                          ),
                        );
                      }
                    });
                  },
                  icon: Icon(
                    _selectedFile != null ? Icons.send : Icons.upload_file,
                    color: AppColors.background,
                  ),
                  label: Text(
                    _selectedFile != null ? _selectedFileName! : 'Wybierz plik',
                    style: const TextStyle(
                      fontSize: 16,
                      color: AppColors.background,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                'Dozwolone typy plików: ${allowedFileTypes.toString()}',
                style: Theme.of(context).textTheme.labelMedium,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 48),
              Text(
                'Leaderboard',
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              _buildLeaderboard(),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildLeaderboard() {
    // Przykładowe dane leaderboard
    final List<Map<String, dynamic>> leaderboardData = [
      {'rank': 1, 'name': 'Jan Kowalski', 'score': 950},
      {'rank': 2, 'name': 'Anna Nowak', 'score': 920},
      {'rank': 3, 'name': 'Piotr Wiśniewski', 'score': 890},
      {'rank': 4, 'name': 'Maria Dąbrowska', 'score': 870},
      {'rank': 5, 'name': 'Tomasz Lewandowski', 'score': 850},
    ];

    return Card(
      elevation: 4,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            // Header
            Container(
              padding: const EdgeInsets.symmetric(vertical: 12),
              decoration: BoxDecoration(
                color: AppColors.primary.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  Expanded(
                    flex: 1,
                    child: Text(
                      '#',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                      textAlign: TextAlign.center,
                    ),
                  ),
                  Expanded(
                    flex: 3,
                    child: Text(
                      'Uczestnik',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                  ),
                  Expanded(
                    flex: 2,
                    child: Text(
                      'Punkty',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                      textAlign: TextAlign.center,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 8),
            // Entries
            ...leaderboardData.map((entry) {
              final isTop3 = entry['rank'] <= 3;
              Color? medalColor;
              if (entry['rank'] == 1) medalColor = Colors.amber;
              if (entry['rank'] == 2) medalColor = Colors.grey[400];
              if (entry['rank'] == 3) medalColor = Colors.brown[300];

              return Container(
                margin: const EdgeInsets.symmetric(vertical: 4),
                padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
                decoration: BoxDecoration(
                  color: isTop3 ? medalColor?.withOpacity(0.1) : null,
                  borderRadius: BorderRadius.circular(8),
                  border: isTop3 
                    ? Border.all(color: medalColor!, width: 2)
                    : null,
                ),
                child: Row(
                  children: [
                    Expanded(
                      flex: 1,
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          if (isTop3)
                            Icon(
                              Icons.emoji_events,
                              color: medalColor,
                              size: 20,
                            ),
                          const SizedBox(width: 4),
                          Text(
                            '${entry['rank']}',
                            style: TextStyle(
                              fontWeight: isTop3 ? FontWeight.bold : FontWeight.normal,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ],
                      ),
                    ),
                    Expanded(
                      flex: 3,
                      child: Text(
                        entry['name'],
                        style: TextStyle(
                          fontWeight: isTop3 ? FontWeight.bold : FontWeight.normal,
                        ),
                      ),
                    ),
                    Expanded(
                      flex: 2,
                      child: Text(
                        '${entry['score']}',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontWeight: FontWeight.w600,
                          color: AppColors.primary,
                        ),
                      ),
                    ),
                  ],
                ),
              );
            }).toList(),
          ],
        ),
      ),
    );
  }
}