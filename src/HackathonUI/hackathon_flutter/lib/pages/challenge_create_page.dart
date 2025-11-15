import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../providers/challenge_provider.dart';
import '../services/token_storage.dart';

class ChallengeCreatePage extends StatefulWidget {
  const ChallengeCreatePage({super.key});

  @override
  State<ChallengeCreatePage> createState() => _ChallengeCreatePageState();
}

class _ChallengeCreatePageState extends State<ChallengeCreatePage> {
  final _formKey = GlobalKey<FormState>();
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  final _metricController = TextEditingController(text: 'accuracy');
  DateTime _selectedDeadline = DateTime.now().add(const Duration(days: 30));
  bool _isSaving = false;

  final String _apiBaseUrl = 'http://localhost:5043/api';

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    _metricController.dispose();
    super.dispose();
  }

  Future<String?> _getToken() => TokenStorage.getToken();

  Future<void> _createChallenge() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isSaving = true);
    final token = await _getToken();
    if (token == null) {
      _showError('No authorization');
      setState(() => _isSaving = false);
      return;
    }

    final Map<String, dynamic> jsonBody = {
      "name": _titleController.text,
      "shortDescription": _descriptionController.text.length > 100
          ? _descriptionController.text.substring(0, 100)
          : _descriptionController.text,
      "fullDescription": _descriptionController.text,
      "rules": "Please submit your solution in CSV format according to instructions.",
      "evaluationMetric": _metricController.text,
      "startDate": DateTime.now().toIso8601String(),
      "endDate": _selectedDeadline.toIso8601String(),
      "maxFileSizeMb": 100,
      "allowedFileTypes": ["csv", "json", "txt"]
    };

    try {
      final response = await http.post(
        Uri.parse('$_apiBaseUrl/Admin/challenges'),
        headers: {
          'Authorization': 'Bearer $token',
          'Content-Type': 'application/json',
        },
        body: json.encode(jsonBody),
      );

      if (response.statusCode == 200 || response.statusCode == 201) {
        if (context.mounted) {
          // Odśwież listę wyzwań
          context.read<ChallengeProvider>().forceRefreshChallenges();
          
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('Challenge created successfully!'),
              backgroundColor: Colors.green,
            ),
          );
          
          // Wróć do dashboardu
          context.go('/dashboard');
        }
      } else {
        _showError('Challenge creation error: ${response.statusCode} - ${response.body}');
      }
    } catch (e) {
      _showError('Network error: $e');
    }
    setState(() => _isSaving = false);
  }

  void _showError(String message) {
    if (!context.mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Create New Challenge'),
        centerTitle: true,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go('/dashboard'),
        ),
      ),
      body: AbsorbPointer(
        absorbing: _isSaving,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16.0),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 800),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const SizedBox(height: 16),
                    
                    // Tytuł
                    TextFormField(
                      controller: _titleController,
                      decoration: const InputDecoration(
                        labelText: 'Challenge Name *',
                        border: OutlineInputBorder(),
                        hintText: 'e.g. Stock Price Prediction',
                      ),
                      validator: (value) => (value == null || value.isEmpty)
                          ? 'Name is required'
                          : null,
                    ),
                    const SizedBox(height: 16),
                    
                    // Opis
                    TextFormField(
                      controller: _descriptionController,
                      decoration: const InputDecoration(
                        labelText: 'Description *',
                        border: OutlineInputBorder(),
                        hintText: 'Detailed challenge description...',
                      ),
                      maxLines: 5,
                      validator: (value) => (value == null || value.isEmpty)
                          ? 'Description is required'
                          : null,
                    ),
                    const SizedBox(height: 16),
                    
                    // Metryka
                    TextFormField(
                      controller: _metricController,
                      decoration: const InputDecoration(
                        labelText: 'Evaluation Metric',
                        border: OutlineInputBorder(),
                        hintText: 'e.g. accuracy, f1-score, rmse',
                      ),
                    ),
                    const SizedBox(height: 16),
                    
                    // Deadline
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        border: Border.all(color: Colors.grey.shade400),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Text(
                            'Deadline *',
                            style: TextStyle(fontSize: 12, color: Colors.grey),
                          ),
                          TextButton.icon(
                            icon: const Icon(Icons.calendar_today),
                            label: Text(
                              DateFormat('yyyy-MM-dd HH:mm').format(_selectedDeadline),
                            ),
                            onPressed: () async {
                              final newDate = await showDatePicker(
                                context: context,
                                initialDate: _selectedDeadline,
                                firstDate: DateTime.now(),
                                lastDate: DateTime.now().add(const Duration(days: 365)),
                              );
                              if (newDate != null) {
                                final newTime = await showTimePicker(
                                  context: context,
                                  initialTime: TimeOfDay.fromDateTime(_selectedDeadline),
                                );
                                if (newTime != null) {
                                  setState(() {
                                    _selectedDeadline = DateTime(
                                      newDate.year,
                                      newDate.month,
                                      newDate.day,
                                      newTime.hour,
                                      newTime.minute,
                                    );
                                  });
                                }
                              }
                            },
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 32),
                    
                    // Przyciski
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        ElevatedButton(
                          onPressed: () => context.go('/dashboard'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.grey,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 32,
                              vertical: 16,
                            ),
                          ),
                          child: const Text('Cancel'),
                        ),
                        const SizedBox(width: 16),
                        ElevatedButton(
                          onPressed: _isSaving ? null : _createChallenge,
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.green,
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 32,
                              vertical: 16,
                            ),
                          ),
                          child: _isSaving
                              ? const SizedBox(
                                  width: 20,
                                  height: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                                  ),
                                )
                              : const Text('Create Challenge'),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    
                    // Informacja
                    const Card(
                      color: Colors.blue,
                      child: Padding(
                        padding: EdgeInsets.all(12.0),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'ℹ️ Information',
                              style: TextStyle(
                                color: Colors.white,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            SizedBox(height: 8),
                            Text(
                              'After creating the challenge, you can add:\n'
                              '• Dataset (training data)\n'
                              '• Answer key (ground truth)\n'
                              'in the challenge edit panel.',
                              style: TextStyle(color: Colors.white),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
