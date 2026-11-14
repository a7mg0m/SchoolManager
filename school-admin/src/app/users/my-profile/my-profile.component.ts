import { Component, OnInit, OnDestroy, Injector, WritableSignal, afterNextRender, signal, computed } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MyProfileService } from '../../core/services/my-profile.service';
import { AuthService } from '../../core/services/auth.service';
import { MyProfile, UpdateMyProfileRequest, MY_PROFILE_ERROR_KEYS } from '../../core/models/my-profile.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/** confirmPassword must equal newPassword */
const passwordsMatch: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('newPassword')?.value || '';
  const confirm = group.get('confirmPassword')?.value || '';
  return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
};

@Component({
  selector: 'app-my-profile',
  standalone: false,
  styleUrl: './my-profile.component.scss',
  templateUrl: './my-profile.component.html',
})
export class MyProfileComponent implements OnInit, OnDestroy {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  profile = signal<MyProfile | null>(null);
  currentLanguage = signal<string>('en');
  isLoading = signal(false);
  isSavingProfile = signal(false);
  isChangingPassword = signal(false);
  showPasswords = signal(false);

  profileError = signal<string | null>(null);
  profileSuccess = signal<string | null>(null);
  passwordError = signal<string | null>(null);
  passwordSuccess = signal<string | null>(null);

  /** New image picked by the user (base64 without prefix), not saved yet */
  pendingImage = signal<string | null>(null);
  /** User asked to remove the current image (applied on save) */
  removeImage = signal(false);
  imageError = signal<string | null>(null);

  readonly passwordMinLength = 6;
  /** Images are resized to this size (px, longest side) before upload */
  private readonly imageMaxSize = 256;
  /** Largest file accepted from the picker (it is resized afterwards) */
  private readonly imageMaxFileBytes = 5 * 1024 * 1024;

  /** Image shown in the avatar: pending > current (unless removed) > none */
  avatarUrl = computed(() => {
    const pending = this.pendingImage();
    if (pending) {
      return `data:image/jpeg;base64,${pending}`;
    }
    const current = this.profile()?.profileImage;
    if (current && !this.removeImage()) {
      return current.startsWith('data:') ? current : `data:${this.detectImageType(current)};base64,${current}`;
    }
    return null;
  });

  initials = computed(() => {
    const p = this.profile();
    const text = [p?.firstName, p?.lastName].filter(Boolean).join(' ') || p?.loginName || '';
    return text.split(' ').filter(Boolean).slice(0, 2).map(w => w.charAt(0).toUpperCase()).join('') || '?';
  });

  roleName = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return (this.currentLanguage() === 'ar' ? p.roleNameAr || p.roleNameEn : p.roleNameEn || p.roleNameAr) || '';
  });

  imageChanged = computed(() => !!this.pendingImage() || this.removeImage());

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private formBuilder: FormBuilder,
    private myProfileService: MyProfileService,
    private authService: AuthService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private injector: Injector
  ) {}

  ngOnInit(): void {
    this.initializeForms();

    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    // "Change password" in the header opens /my-profile#change-password (also while already here)
    this.activatedRoute.fragment
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        if (this.profile()) {
          this.scrollToFragment();
        }
      });

    this.loadProfile();
  }

  /** Scroll to the section named in the URL fragment, after the page has rendered */
  private scrollToFragment(): void {
    const fragment = this.activatedRoute.snapshot.fragment;
    if (!fragment) {
      return;
    }
    afterNextRender(() => {
      document.getElementById(fragment)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      if (fragment === 'change-password') {
        document.getElementById('currentPassword')?.focus({ preventScroll: true });
      }
    }, { injector: this.injector });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initializeForms(): void {
    this.profileForm = this.formBuilder.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      phoneNumber: ['', [Validators.maxLength(50)]],
      address: ['', [Validators.maxLength(500)]]
    });

    this.passwordForm = this.formBuilder.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(this.passwordMinLength)]],
      confirmPassword: ['', Validators.required]
    }, { validators: passwordsMatch });
  }

  // ---------------------------------------------------------------------------
  // Profile
  // ---------------------------------------------------------------------------

  loadProfile(): void {
    this.isLoading.set(true);
    this.profileError.set(null);

    this.myProfileService
      .getMyProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.applyProfile(response.data);
          } else {
            this.profileError.set(response.message || this.translate.instant('myProfile.loadFailed'));
          }
          this.isLoading.set(false);
          this.scrollToFragment();
        },
        error: (error) => {
          console.error('Error loading my profile:', error);
          this.profileError.set(this.apiError(error, 'myProfile.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /** Show a profile from the API in the page and the form */
  private applyProfile(profile: MyProfile): void {
    this.profile.set(profile);
    this.pendingImage.set(null);
    this.removeImage.set(false);
    this.imageError.set(null);
    this.profileForm.reset({
      firstName: profile.firstName,
      lastName: profile.lastName,
      email: profile.email,
      phoneNumber: profile.phoneNumber ?? '',
      address: profile.address ?? ''
    });
  }

  saveProfile(): void {
    if (this.isSavingProfile()) {
      return;
    }
    if (!this.profileForm.valid) {
      this.profileForm.markAllAsTouched();
      this.profileError.set(this.translate.instant('myProfile.invalidForm'));
      return;
    }

    const v = this.profileForm.getRawValue();
    const request: UpdateMyProfileRequest = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      email: v.email.trim(),
      phoneNumber: v.phoneNumber?.trim() || null,
      address: v.address?.trim() || null,
      profileImage: this.pendingImage(),
      removeProfileImage: this.removeImage() && !this.pendingImage()
    };

    this.isSavingProfile.set(true);
    this.profileError.set(null);

    this.myProfileService
      .updateMyProfile(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.applyProfile(response.data);
            this.syncHeader(response.data);
            this.flash(this.profileSuccess, this.translate.instant('myProfile.profileSaved'));
          } else {
            this.profileError.set(response.message || this.translate.instant('myProfile.saveFailed'));
          }
          this.isSavingProfile.set(false);
        },
        error: (error) => {
          console.error('Error saving my profile:', error);
          this.profileError.set(this.apiError(error, 'myProfile.saveFailed'));
          this.isSavingProfile.set(false);
        }
      });
  }

  /** Discard unsaved changes in the profile form and image */
  resetProfile(): void {
    const p = this.profile();
    if (p) {
      this.applyProfile(p);
    }
    this.profileError.set(null);
  }

  /** Update the user info kept by AuthService, so the header shows the new name / image */
  private syncHeader(p: MyProfile): void {
    this.authService.updateUserProfile({
      id: p.id ?? '',
      userId: p.userId,
      firstName: p.firstName,
      lastName: p.lastName,
      email: p.email,
      phoneNumber: p.phoneNumber,
      address: p.address,
      profileImage: p.profileImage
    });
  }

  // ---------------------------------------------------------------------------
  // Image
  // ---------------------------------------------------------------------------

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = ''; // allow picking the same file again
    this.imageError.set(null);

    if (!file) {
      return;
    }
    if (!file.type.startsWith('image/')) {
      this.imageError.set(this.translate.instant('myProfile.imageNotImage'));
      return;
    }
    if (file.size > this.imageMaxFileBytes) {
      this.imageError.set(this.translate.instant('myProfile.imageTooLarge', { max: 5 }));
      return;
    }

    this.resizeImage(file)
      .then(base64 => {
        this.pendingImage.set(base64);
        this.removeImage.set(false);
      })
      .catch(() => this.imageError.set(this.translate.instant('myProfile.imageReadFailed')));
  }

  removeCurrentImage(): void {
    this.pendingImage.set(null);
    this.removeImage.set(true);
    this.imageError.set(null);
  }

  /**
   * Resize to imageMaxSize (longest side) and encode as JPEG, so the upload and the
   * copy kept in localStorage stay small. Resolves base64 without the "data:" prefix.
   */
  private resizeImage(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onerror = () => reject(reader.error);
      reader.onload = () => {
        const img = new Image();
        img.onerror = () => reject(new Error('invalid image'));
        img.onload = () => {
          const scale = Math.min(1, this.imageMaxSize / Math.max(img.width, img.height));
          const canvas = document.createElement('canvas');
          canvas.width = Math.max(1, Math.round(img.width * scale));
          canvas.height = Math.max(1, Math.round(img.height * scale));
          const ctx = canvas.getContext('2d');
          if (!ctx) {
            reject(new Error('canvas not supported'));
            return;
          }
          // White background so transparent PNGs don't turn black in JPEG
          ctx.fillStyle = '#fff';
          ctx.fillRect(0, 0, canvas.width, canvas.height);
          ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
          resolve(canvas.toDataURL('image/jpeg', 0.85).split(',')[1]);
        };
        img.src = reader.result as string;
      };
      reader.readAsDataURL(file);
    });
  }

  // ---------------------------------------------------------------------------
  // Password
  // ---------------------------------------------------------------------------

  changePassword(): void {
    if (this.isChangingPassword()) {
      return;
    }
    if (!this.passwordForm.valid) {
      this.passwordForm.markAllAsTouched();
      this.passwordError.set(this.translate.instant('myProfile.invalidForm'));
      return;
    }

    const v = this.passwordForm.getRawValue();
    this.isChangingPassword.set(true);
    this.passwordError.set(null);

    this.myProfileService
      .changePassword({ currentPassword: v.currentPassword, newPassword: v.newPassword })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.passwordForm.reset();
            this.showPasswords.set(false);
            this.flash(this.passwordSuccess, this.translate.instant('myProfile.passwordChanged'));
          } else {
            this.passwordError.set(response.message || this.translate.instant('myProfile.passwordFailed'));
          }
          this.isChangingPassword.set(false);
        },
        error: (error) => {
          console.error('Error changing password:', error);
          this.passwordError.set(this.apiError(error, 'myProfile.passwordFailed'));
          this.isChangingPassword.set(false);
        }
      });
  }

  togglePasswords(): void {
    this.showPasswords.update(v => !v);
  }

  get passwordMismatch(): boolean {
    const confirm = this.passwordForm.get('confirmPassword');
    return this.passwordForm.hasError('passwordMismatch') && !!confirm && (confirm.dirty || confirm.touched);
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  hasError(form: FormGroup, fieldName: string, errorType: string): boolean {
    const control = form.get(fieldName);
    return !!(control && control.hasError(errorType) && (control.dirty || control.touched));
  }

  /** Show a success message for 4 seconds */
  private flash(target: WritableSignal<string | null>, message: string): void {
    target.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => target.set(null));
  }

  private detectImageType(base64: string): string {
    if (base64.startsWith('/9j/')) return 'image/jpeg';
    if (base64.startsWith('iVBORw0KGgo')) return 'image/png';
    if (base64.startsWith('R0lGOD')) return 'image/gif';
    if (base64.startsWith('UklGR')) return 'image/webp';
    return 'image/png';
  }

  /**
   * Translate a known API error key (e.g. CurrentPasswordInvalid), otherwise use the fallback
   */
  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (MY_PROFILE_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`myProfile.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
