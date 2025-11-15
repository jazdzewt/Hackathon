import 'package:flutter/material.dart';
import 'package:hackathon_flutter/theme/colors.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import '../services/token_storage.dart';

class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class LoginTextStyle {
  static const TextStyle header = TextStyle(
    fontSize: 28,
    fontWeight: FontWeight.bold,
  );
}

class _RegisterScreenState extends State<RegisterScreen> {
  final TextEditingController emailController = TextEditingController();
  final TextEditingController passwordController = TextEditingController();
  final TextEditingController usernameController = TextEditingController();
  final TextEditingController passwordConfirmationController =
      TextEditingController();
  bool _isLoading = false;

  String extractMsg(String error) {
    try {
      final jsonStart = error.indexOf('{');
      if (jsonStart == -1) return error;
      final jsonPart = error.substring(jsonStart);
      final parsed = jsonDecode(jsonPart);
      if (parsed is Map<String, dynamic> && parsed.containsKey('msg')) {
        return parsed['msg'];
      }
      return error;
    } catch (e) {
      return error;
    }
  }

  Future<bool> registerUser({
    required String username,
    required String email,
    required String password,
    required String passwordConfirmation,
  }) async {
  final url = Uri.parse('http://localhost:5043/api/Auth/register');

    setState(() => _isLoading = true);

    try {
      final response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({
              'email': email,
              'password': password,
              'confirmPassword': passwordConfirmation,
              'username': username,
            }),
          )
          .timeout(const Duration(seconds: 12));

      if (response.statusCode >= 200 && response.statusCode < 300) {
        final data = jsonDecode(response.body);
        debugPrint('Response data: $data');
        
        if (data is Map<String, dynamic> && data.containsKey('accessToken')) {
          await TokenStorage.saveToken(data['accessToken']);
          debugPrint('Token saved: ${data['accessToken']}');
          
          if (data.containsKey('refreshToken')) {
            await TokenStorage.saveRefreshToken(data['refreshToken']);
          }
          
          final savedToken = await TokenStorage.getToken();
          debugPrint('Token read: $savedToken');
        }
        
        if (!mounted) return false;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Registration successful!'),
            backgroundColor: Colors.green,
          ),
        );
        return true;
      }
      final data = jsonDecode(response.body);
      String errorMessage = 'Registration failed';
      if (data is Map<String, dynamic>) {
        if (data.containsKey('error')) {
          errorMessage = extractMsg(data['error'].toString());
        } else if (data.containsKey('message')) {
          errorMessage = data['message'];
        }
      }

      if (!mounted) return false;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(errorMessage), backgroundColor: Colors.red),
      );
      return false;
    } catch (e) {
      if (!mounted) return false;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Registration exception: $e'), backgroundColor: Colors.red),
      );
      return false;
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }



  @override
  void dispose() {
    emailController.dispose();
    passwordController.dispose();
    super.dispose();
  }

  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  bool _submitted = false;
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Company.example')),
      body: Row(
        children: [
          Expanded(
            flex: 1,
            child: Container(
              height: double.infinity,
              child: Image.asset(
                'assets/images/hackaton2.jpg',
                fit: BoxFit.cover,
              ),
            ),
          ),
          Expanded(
            flex: 1,
            child: LayoutBuilder(
              builder: (context, constraints) {
                final bodyHeight = constraints.maxHeight;

                return SingleChildScrollView(
                  child: ConstrainedBox(
                    constraints: BoxConstraints(minHeight: bodyHeight),
                    child: IntrinsicHeight(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(height: bodyHeight * 0.02),

                          Center(
                            child: Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 16),
                              child: Text(
                                'Welcome to Company.example Hackathons!',
                                style: Theme.of(context).textTheme.headlineLarge,
                                textAlign: TextAlign.center,
                              ),
                            ),
                          ),

                          const Spacer(),

                          Align(
                            alignment: Alignment.center,
                            child: Container(
                              width: 400,
                              padding: const EdgeInsets.all(32),
                              decoration: BoxDecoration(
                                color: AppColors.background,
                                borderRadius: BorderRadius.circular(12),
                                boxShadow: [
                                  BoxShadow(
                                    color: AppColors.shadows,
                                    blurRadius: 16,
                                    offset: const Offset(0, 8),
                                  ),
                                ],
                              ),
                              child: Form(
                                key: _formKey,
                                autovalidateMode: _submitted
                                    ? AutovalidateMode.always
                                    : AutovalidateMode.disabled,
                                child: Column(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Text(
                                      'Registration',
                                      style: LoginTextStyle.header,
                                    ),
                                    const SizedBox(height: 24),
                                    TextFormField(
                                      controller: usernameController,
                                      decoration: const InputDecoration(
                                        labelText: 'Username',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Username is required';
                                        }
                                        return null;
                                      },
                                    ),
                                    SizedBox(height: 16),
                                    TextFormField(
                                      controller: emailController,
                                      decoration: const InputDecoration(
                                        labelText: 'Email',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Email address is required';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 16),
                                    TextFormField(
                                      controller: passwordController,
                                      obscureText: true,
                                      decoration: const InputDecoration(
                                        labelText: 'Password',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Password is required';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 16),
                                    TextFormField(
                                      controller:
                                          passwordConfirmationController,
                                      obscureText: true,
                                      decoration: const InputDecoration(
                                        labelText: 'Confirm password',
                                        border: OutlineInputBorder(),
                                      ),
                                      validator: (value) {
                                        if (value == null ||
                                            value.trim().isEmpty) {
                                          return 'Password is required';
                                        }
                                        if (value != passwordController.text) {
                                          return 'Passwords do not match';
                                        }
                                        return null;
                                      },
                                    ),
                                    const SizedBox(height: 24),
                                    SizedBox(
                                      width: double.infinity,
                                      child: ElevatedButton(
                                        style: ElevatedButton.styleFrom(
                                          backgroundColor: AppColors.primary,
                                        ),
                                        onPressed: _isLoading
                                            ? null
                                            : () async {
                                          setState(() {
                                            _submitted = true;
                                          });
                                          if (_formKey.currentState
                                                  ?.validate() ==
                                              true) {
                                            final username = usernameController
                                                .text
                                                .trim();
                                            final email = emailController.text
                                                .trim();
                                            final password =
                                                passwordController.text;
                                            final passwordConfirmation =
                                                passwordConfirmationController
                                                    .text;
                                            setState(() {
                                              _isLoading = true;
                                            });
                                            final success = await registerUser(
                                              username: username,
                                              email: email,
                                              password: password,
                                              passwordConfirmation: passwordConfirmation,
                                            );
                                            if (!mounted) return;
                                            setState(() {
                                              _isLoading = false;
                                            });
                                            if (success) {
                                              GoRouter.of(context).go('/dashboard');
                                            }
                                          }
                                        },
                                        child: _isLoading
                                            ? const SizedBox(
                                                width: 18,
                                                height: 18,
                                                child: CircularProgressIndicator(
                                                  strokeWidth: 2,
                                                  valueColor: AlwaysStoppedAnimation<Color>(AppColors.background),
                                                ),
                                              )
                                            : const Text(
                                                'Register',
                                                style: TextStyle(
                                                  color: AppColors.background,
                                                ),
                                              ),
                                      ),
                                    ),
                                    const SizedBox(height: 12),
                                    TextButton(
                                      style:
                                          TextButton.styleFrom(
                                            padding: EdgeInsets.zero,
                                            minimumSize: const Size(0, 0),
                                            tapTargetSize: MaterialTapTargetSize
                                                .shrinkWrap,
                                            foregroundColor: AppColors.primary,
                                            splashFactory:
                                                NoSplash.splashFactory,
                                          ).copyWith(
                                            overlayColor: WidgetStateProperty.all(
                                              Colors.transparent,
                                            ),
                                          ),
                                      onPressed: () {
                                        GoRouter.of(context).go('/');
                                      },
                                      child: const Text(
                                        'Already have an account',
                                        style: TextStyle(
                                          decoration: TextDecoration.underline,
                                          fontSize: 14,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),

                          const Spacer(),
                        ],
                      ),
                    ),
                  ),
                );
              },
            ),
          ),
        ],
      ),
      backgroundColor: AppColors.background,
    );
  }
}
